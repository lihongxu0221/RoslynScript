using BgCommon.Script.Compiler.Configs;
using BgLogger;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Runtime.ConstrainedExecution;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 脚本管理器类，负责管理脚本上下文集合、处理生命周期事件及提供编辑执行接口.
/// </summary>
public sealed class ScriptManager : IDisposable
{
    /// <summary>
    /// 脚本配置信息.
    /// </summary>
    private readonly ScriptConfig config;
    private readonly SynchronizationContext? syncContext = SynchronizationContext.Current;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> debounceTokens = new();
    private FileSystemWatcher? watcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptManager"/> class.
    /// </summary>
    /// <param name="config">脚本配置信息对象.</param>
    public ScriptManager(ScriptConfig config)
    {
        // 验证配置参数是否为空.
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        this.config = config;

        // 初始化脚本上下文集合.
        // 订阅集合变更事件以管理上下文事件.
        this.Contexts = new ObservableCollection<ScriptContext>();
        this.Contexts.CollectionChanged += this.Contexts_CollectionChanged;
    }

    /// <summary>
    /// Gets 脚本配置信息.
    /// </summary>
    public ScriptConfig Config => this.config;

    /// <summary>
    /// Gets 脚本上下文集合.
    /// </summary>
    public ObservableCollection<ScriptContext> Contexts { get; private set; }

    /// <summary>
    /// 模糊搜索脚本（按名称或摘要）.
    /// </summary>
    /// <param name="keyword">名称或摘要.</param>
    /// <returns>返回满足条件的脚本上下文列表.</returns>
    public IEnumerable<ScriptContext> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return this.Contexts;
        }

        return this.Contexts.Where(c =>
            c.ScriptName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            c.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 尝试获取指定名称的脚本上下文.
    /// </summary>
    /// <param name="scriptName">脚本名称.</param>
    /// <returns>返回找到的脚本上下文，未找到则返回 null.</returns>
    public ScriptContext? GetContext(string scriptName) =>
        this.Contexts.FirstOrDefault(c => c.ScriptName.Equals(scriptName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 检查是否存在重名脚本.
    /// </summary>
    /// <param name="scriptName">脚本名称.</param>
    /// <returns>返回是否存在同名脚本的布尔值.</returns>
    public bool Exists(string scriptName) =>
        this.Contexts.Any(c => c.ScriptName.Equals(scriptName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 启用文件系统监听，当脚本文件夹中的文件被外部修改、删除或新增时自动同步.
    /// </summary>
    public void EnableFileWatcher()
    {
        if (watcher == null)
        {
            watcher = new FileSystemWatcher(this.config.ScriptPath, $"*.{this.config.ScriptFileExtension}")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            };

            watcher.Changed += this.OnFileChanged;
            watcher.Created += this.OnFileCreated;
            watcher.Deleted += this.OnFileDeleted;
            watcher.Renamed += this.OnFileRenamed;
        }
    }

    #region 文件监听回调逻辑

    private async void OnFileCreated(object? sender, FileSystemEventArgs e)
    {
        var name = Path.GetFileNameWithoutExtension(e.Name);
        if (string.IsNullOrEmpty(name) || this.Exists(name))
        {
            return;
        }

        // 需要切回 UI 线程，这里假设使用了合适的同步机制或 ObservableCollection 处理了跨线程
        var context = new ScriptContext(name, this.config, false);
        await context.LoadAsync();

        RunOnUIThread(() => this.Contexts.Add(context));
    }

    private async void OnFileChanged(object? sender, FileSystemEventArgs e)
    {
        var name = Path.GetFileNameWithoutExtension(e.Name);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        // 取消之前的延迟任务
        if (debounceTokens.TryRemove(name, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        debounceTokens[name] = cts;

        _ = Task.Delay(500, cts.Token).ContinueWith(
            async t =>
            {
                if (t.IsCanceled)
                {
                    return;
                }

                var context = GetContext(name);
                if (context != null && !context.IsDirty)
                {
                    await context.LoadAsync();
                    LogRun.Info($"脚本 [{name}] 已通过文件系统同步自动重载。");
                }

                debounceTokens.TryRemove(name, out _);
            });
    }

    private void OnFileDeleted(object? sender, FileSystemEventArgs e)
    {
        var name = Path.GetFileNameWithoutExtension(e.Name);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var context = GetContext(name);
        if (context != null)
        {
            RunOnUIThread(() => this.Contexts.Remove(context));
        }
    }

    private void OnFileRenamed(object? sender, RenamedEventArgs e)
    {
        var oldName = Path.GetFileNameWithoutExtension(e.OldName);
        var newName = Path.GetFileNameWithoutExtension(e.Name);
        if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
        {
            return;
        }

        // 在 UI 线程中执行集合和属性变更
        RunOnUIThread(() =>
        {
            var context = GetContext(oldName);
            if (context != null)
            {
                // 同步更新内部名称
                context.UpdateNameInternal(newName);

                // 触发事件通知 UI 或日志系统路径已变
                LogRun.Info($"检测到脚本更名: {oldName} -> {newName}");
            }
        });
    }

    #endregion

    /// <summary>
    /// 从配置的脚本路径加载所有现有的 .cs 脚本文件.
    /// </summary>
    /// <returns>返回加载的结果.</returns>
    public async Task LoadAllScriptsAsync()
    {
        if (!Directory.Exists(this.config.ScriptPath))
        {
            Directory.CreateDirectory(this.config.ScriptPath);
            return;
        }

        var files = Directory.GetFiles(this.config.ScriptPath, $"*.{config.ScriptFileExtension}");
        foreach (var file in files)
        {
            // 避免重复加载
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (this.Contexts.Any(c => c.ScriptName.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var context = new ScriptContext(fileName, this.config, false);
            await context.LoadAsync();
            this.Contexts.Add(context);
        }
    }

    /// <summary>
    /// 异步保存所有已修改且未保存的脚本.
    /// </summary>
    /// <returns>返回保存的结果.</returns>
    public async Task SaveAllDirtyScriptsAsync()
    {
        var dirtyContexts = this.Contexts.Where(c => c.IsDirty && !string.IsNullOrEmpty(c.ScriptName)).ToList();
        foreach (var context in dirtyContexts)
        {
            await context.SaveAsync();
        }
    }

    /// <summary>
    /// 根据模板创建一个新的脚本上下文（尚未保存到磁盘）.
    /// </summary>
    /// <param name="templateName">模板名称.</param>
    /// <returns>返回初始化的脚本上下文.</returns>
    public async Task<ScriptContext?> CreateFromTemplateAsync(string templateName)
    {
        var context = new ScriptContext(templateName, this.config, true);
        await context.LoadAsync();

        // 模板创建后添加到集合，此时 ScriptName 为空，直到 SaveAsync(newName)
        this.Contexts.Add(context);
        return context;
    }

    /// <summary>
    /// 删除脚本：从内存移除并物理删除文件.
    /// </summary>
    /// <param name="scriptName">脚本名称.</param>
    /// <returns>返回删除的结果.</returns>
    public async Task<bool> DeleteScriptAsync(string scriptName)
    {
        var context = this.GetContext(scriptName);
        if (context == null)
        {
            return false;
        }

        string path = context.ScriptFilePath;
        this.Contexts.Remove(context);
        context.Dispose();

        if (File.Exists(path))
        {
            await Task.Run(() => File.Delete(path));
        }

        return true;
    }

    /// <summary>
    /// 异步执行指定的脚本.
    /// </summary>
    /// <param name="scriptName">脚本名称.</param>
    /// <param name="globals">传递给脚本的上下文对象（建议使用 ScriptGlobals 子类）.</param>
    /// <param name="data">传递给脚本的初始数据.</param>
    /// <param name="ct">取消令牌.</param>
    /// <returns>返回脚本执行的结果.</returns>
    public async Task<ScriptResult> ExecuteAsync(string scriptName, ScriptGlobals? globals = null, object? data = null, CancellationToken ct = default)
    {
        var ctx = this.GetContext(scriptName);
        if (ctx == null)
        {
            return new ScriptResult(false, $"找不到名为 '{scriptName}' 的脚本.");
        }

        // 如果未提供 globals 实例，则使用约定好的默认实现
        globals ??= new DefaultScriptGlobals();
        globals.Data = data;
        globals.Log = (msg) => LogRun.Info($"[{scriptName}] {msg}");
        globals.CancellationToken = ct;

        try
        {
            // 调用 ScriptContext.RunAsync，它会触发 globals.ExecuteStateAsync
            var scriptValue = await ctx.RunAsync(globals, ct);
            return scriptValue;
        }
        catch (Exception ex)
        {
            LogRun.Error($"脚本 {scriptName} 执行异常: {ex.Message}");
            return new ScriptResult(
                    success: false,
                    message: $"执行异常: {ex.Message}",
                    result: null,
                    exception: ex,
                    inputs: globals.Data,
                    outputs: globals.Outputs);
        }
    }

    /// <summary>
    /// 显示脚本编辑器.
    /// </summary>
    public void ShowEditor()
    {
        // 方法体逻辑保持原样.
    }

    /// <summary>
    /// 显示指定脚本的编辑器.
    /// </summary>
    /// <param name="scriptName">脚本名称.</param>
    public void ShowEditor(string scriptName)
    {
        // 验证脚本名称.
        ArgumentNullException.ThrowIfNull(scriptName, nameof(scriptName));
    }

    /// <summary>
    /// 释放管理器占用的资源，并注销所有上下文事件.
    /// </summary>
    public void Dispose()
    {
        if (this.watcher != null)
        {
            this.watcher.Changed -= this.OnFileChanged;
            this.watcher.Created -= this.OnFileCreated;
            this.watcher.Deleted -= this.OnFileDeleted;
            this.watcher.Renamed -= this.OnFileRenamed;
            this.watcher.EnableRaisingEvents = false;
            this.watcher.Dispose();
        }

        if (this.Contexts != null)
        {
            // 循环注销所有上下文的事件处理程序.
            for (int i = 0; i < this.Contexts.Count; i++)
            {
                this.HanlderEvent(this.Contexts[i], false);
                this.Contexts[i].Dispose();
            }

            // 注销集合变更事件.
            this.Contexts.CollectionChanged -= this.Contexts_CollectionChanged;
        }

        // 通知 GC 不需要调用终结器.
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 响应上下文集合变更事件.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="eventArgs">变更事件参数.</param>
    private void Contexts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        // 处理新增项的事件注册.
        if (eventArgs.Action == NotifyCollectionChangedAction.Add && eventArgs.NewItems != null)
        {
            foreach (var newItem in eventArgs.NewItems)
            {
                if (newItem is ScriptContext addedContext)
                {
                    this.HanlderEvent(addedContext, true);
                }
            }

            return;
        }

        // 处理删除项的事件注销.
        if (eventArgs.Action == NotifyCollectionChangedAction.Remove && eventArgs.OldItems != null)
        {
            foreach (var oldItem in eventArgs.OldItems)
            {
                if (oldItem is ScriptContext removedContext)
                {
                    this.HanlderEvent(removedContext, false);
                }
            }
        }
    }

    /// <summary>
    /// 批量注册或注销脚本上下文的事件处理程序.
    /// </summary>
    /// <param name="context">脚本上下文实例.</param>
    /// <param name="isSubscribing">指示是注册 (true) 还是注销 (false) 事件.</param>
    private void HanlderEvent(ScriptContext context, bool isSubscribing)
    {
        // 验证上下文对象.
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (isSubscribing)
        {
            // 注册所有相关生命周期事件.
            context.OnDispose += this.Context_OnDispose;
            context.OnInitialized += this.Context_OnInitialized;
            context.OnError += this.Context_OnError;
            context.OnCompiling += this.Context_OnCompiling;
            context.OnCompiled += this.Context_OnCompiled;
            context.OnRunning += this.Context_OnRunning;
            context.OnRuned += this.Context_OnRuned;
            context.OnSaved += this.Context_OnSaved;
        }
        else
        {
            // 注销所有相关生命周期事件.
            context.OnDispose -= this.Context_OnDispose;
            context.OnInitialized -= this.Context_OnInitialized;
            context.OnError -= this.Context_OnError;
            context.OnCompiling -= this.Context_OnCompiling;
            context.OnCompiled -= this.Context_OnCompiled;
            context.OnRunning -= this.Context_OnRunning;
            context.OnRuned -= this.Context_OnRuned;
            context.OnSaved -= this.Context_OnSaved;
        }
    }

    /// <summary>
    /// 上下文初始化完成时的回调.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="e">事件参数.</param>
    private void Context_OnInitialized(object? sender, ScriptFileEventArgs e)
    {
    }

    /// <summary>
    /// 上下文资源释放时的回调.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="eventArgs">事件参数.</param>
    private void Context_OnDispose(object? sender, EventArgs eventArgs)
    {
        if (sender is ScriptContext ctx)
        {
            // 当上下文释放时，注销其在该管理器中的事件订阅.
            this.Contexts.Remove(ctx);
            this.HanlderEvent(ctx, false);
        }
    }

    /// <summary>
    /// 脚本保存后的回调.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="e">事件参数.</param>
    private void Context_OnSaved(object? sender, ScriptFileEventArgs e)
    {
    }

    /// <summary>
    /// 脚本发生错误时的回调.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="e">事件参数.</param>
    private void Context_OnError(object? sender, ScriptErrorEventArgs e)
    {
    }

    /// <summary>
    /// 脚本开始编译时的回调.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="eventArgs">事件参数.</param>
    private void Context_OnCompiling(object? sender, EventArgs eventArgs)
    {
    }

    /// <summary>
    /// 脚本编译完成时的回调.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="e">事件参数.</param>
    private void Context_OnCompiled(object? sender, ScriptCompilationEventArgs e)
    {
        if (!e.IsSuccess)
        {
            foreach (var diag in e.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                LogRun.Error($"编译错误: {diag.GetMessage()} ({e.CompilationDuration.TotalMilliseconds}ms)");
            }
        }
    }

    /// <summary>
    /// 脚本开始运行时的回调.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="eventArgs">事件参数.</param>
    private void Context_OnRunning(object? sender, EventArgs eventArgs)
    {
    }

    /// <summary>
    /// 脚本运行结束后的回调.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="e">事件参数.</param>
    private void Context_OnRuned(object? sender, ScriptExecutionEventArgs e)
    {
        if (e.HasError)
        {
            LogRun.Error($"运行失败: {e.Exception?.Message}");
        }
        else
        {
            LogRun.Info($"运行成功，返回结果: {e.Result} (耗时: {e.ExecutionDuration.TotalMilliseconds}ms)");
        }
    }

    private void RunOnUIThread(Action action)
    {
        if (syncContext != null)
        {
            syncContext.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    /// <summary>
    /// 由于首次执行时 C# 脚本引擎需要加载和编译相关程序集，可能会有较长的延迟。调用此方法可以提前加载引擎，减少后续执行的响应时间.
    /// </summary>
    /// <returns>预热任务.</returns>
    public static async Task WarmUpAsync()
    {
        await Task.Run(() => CSharpScript.EvaluateAsync("1 + 1"));
    }
}