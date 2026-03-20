using BgCommon.Script.Runtime.Compilation;
using BgCommon.Script.Runtime.Compilation.Exceptions;
using BgCommon.Script.Runtime.Configuration;
using BgCommon.Script.Runtime.Models;

namespace BgCommon.Script.Runtime;

/// <summary>
/// 脚本上下文类，集成 Roslyn 脚本引擎实现代码的加载、编译与执行.
/// </summary>
public sealed class ScriptContext : ObservableObject, IDisposable
{
    private readonly ICodeCompiler compiler; // 编译器
    private readonly CompilationInput input;
    private readonly SemaphoreSlim runLock = new(1, 1); // 初始化信号量，允许 1 个并发。
    private readonly Guid id = Guid.NewGuid();
    private string code = string.Empty;
    private string? cachedSummary;
    private string summarySourceCode = string.Empty;
    private bool isNeedReCompile = true; // 是否需要重新编译.
    private bool isDirty;
    private bool isRunning;
    private CancellationTokenSource? internalCts;

    private string scriptName = string.Empty;
    private ScriptFile? script;
    private ConfigurationMgr<ScriptFile>? scriptFileMgr;
    private bool isInternalSyncing; // 标记位

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptContext"/> class.
    /// </summary>
    /// <param name="name">脚本名称.</param>
    /// <param name="config">脚本配置信息.</param>
    /// <param name="compiler">可以编译器.</param>
    public ScriptContext(
        string name,
        ScriptConfig config,
        ICodeCompiler compiler)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        ArgumentNullException.ThrowIfNull(compiler, nameof(compiler));

        this.compiler = compiler;
        this.ScriptName = name;
        this.ScriptFilePath = config.GetFilePath(name);
        this.ReferLibsDirectory = config.LibraryRefDirectory;
        this.Config = config;
        this.Namespaces.Clear();
        this.Namespaces.AddRange(config.Usings.Distinct());
        this.References.Clear();
        this.References.AddRange(config.References);
        this.input = new CompilationInput(
            DotNet.DotNetFrameworkVersion.DotNet8,
            references: config.References.ToHashSet(),
            usings: config.Usings.ToHashSet());
        this.IsDirty = false;
        this.IsRunning = false;
    }

    /// <summary>
    /// Gets 脚本上下文Id.
    /// </summary>
    public Guid Id => this.id;

    /// <summary>
    /// Gets 脚本名称.
    /// </summary>
    public string ScriptName
    {
        get => this.scriptName;
        private set => this.SetProperty(ref this.scriptName, value);
    }

    /// <summary>
    /// Gets 脚本文件的物理存储路径.
    /// </summary>
    public FilePath ScriptFilePath { get; private set; }

    /// <summary>
    /// Gets 脚本配置.
    /// </summary>
    public ScriptConfig Config { get; private set; }

    /// <summary>
    /// Gets 脚本预定义的命名空间列表.
    /// </summary>
    public ObservableCollection<string> Namespaces { get; } = new ObservableCollection<string>();

    /// <summary>
    /// Gets 引用程序集所在文件夹.
    /// </summary>
    public DirectoryPath ReferLibsDirectory { get; }

    /// <summary>
    /// Gets 脚本需要引用的外部程序集列表.
    /// </summary>
    public ObservableCollection<LibraryRef> References { get; } = new ObservableCollection<LibraryRef>();

    /// <summary>
    /// Gets 脚本文件元数据.
    /// </summary>
    public ScriptFile Script => this.script.NotNull();

    /// <summary>
    /// Gets 脚本的代码内容.
    /// </summary>
    public string Code => this.code;

    /// <summary>
    /// Gets 从脚本代码中解析出的摘要说明（自动从顶部注释提取）.
    /// </summary>
    public string Summary
    {
        get
        {
            // 只有当代码发生过变化时，才重新解析摘要
            if (this.cachedSummary == null || this.summarySourceCode != this.Code)
            {
                this.summarySourceCode = this.Code;
                this.cachedSummary = MetadataParser.GetSummary(this.Code);
            }

            return this.cachedSummary;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether 脚本内容是否已更改且未保存.
    /// </summary>
    public bool IsDirty
    {
        get => this.isDirty;
        set => this.SetProperty(ref this.isDirty, value);
    }

    /// <summary>
    /// Gets a value indicating whether 脚本是否正在运行.
    /// </summary>
    public bool IsRunning
    {
        get => this.isRunning;
        private set => this.SetProperty(ref this.isRunning, value);
    }

    /// <summary>
    /// Gets or sets 操作的默认超时时间间隔.
    /// </summary>
    /// <remarks>默认值为30秒。调整此属性可控制操作在超时前允许运行的时长.</remarks>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 当对象释放时触发.
    /// </summary>
    public event EventHandler? OnDispose;

    /// <summary>
    /// 当脚本环境加载/初始化完成时触发.
    /// </summary>
    public event EventHandler<ScriptFileEventArgs>? OnInitialized;

    /// <summary>
    /// 当脚本保存完成时触发.
    /// </summary>
    public event EventHandler<ScriptFileEventArgs>? OnSaved;

    /// <summary>
    /// 当脚本发生任何阶段的错误时触发.
    /// </summary>
    public event EventHandler<ScriptErrorEventArgs>? OnError;

    /// <summary>
    /// 当脚本开始编译时触发.
    /// </summary>
    public event EventHandler? OnCompiling;

    /// <summary>
    /// 当脚本编译结束（无论成功失败）时触发.
    /// </summary>
    public event EventHandler<ScriptCompilationEventArgs>? OnCompiled;

    /// <summary>
    /// 当脚本开始运行时触发.
    /// </summary>
    public event EventHandler? OnRunning;

    /// <summary>
    /// 当脚本运行结束时触发.
    /// </summary>
    public event EventHandler<ScriptExecutionEventArgs>? OnRuned;

    /// <summary>
    /// 异步加载脚本内容. 模板模式从模板加载，普通模式从物理文件加载.
    /// </summary>
    /// <returns>表示异步操作的任务.</returns>
    public async Task LoadAsync()
    {
        try
        {
            // 加载 ScriptFile 元数据
            this.scriptFileMgr = new ConfigurationMgr<ScriptFile>(this.ScriptFilePath);
            if (!this.scriptFileMgr.LoadFromFile())
            {
                this.scriptFileMgr.Entity = new ScriptFile(this.ScriptName);
                await this.scriptFileMgr.SaveToFileAsync();
            }

            this.SubscribeMetadata(this.scriptFileMgr.Entity);
            if (this.Script != null && !string.IsNullOrEmpty(this.Script.Content))
            {
                // 使用 ScriptFile 中的内容
                this.code = this.Script.Content;
                this.IsDirty = false;
                this.OnInitialized?.Invoke(this, new ScriptFileEventArgs(
                    ScriptFileAction.Loaded,
                    this.ScriptName,
                    this.ScriptFilePath));
            }
        }
        catch (Exception ex)
        {
            this.OnError?.Invoke(this, new ScriptErrorEventArgs(
                ScriptErrorSource.Loading,
                ex,
                "加载脚本失败"));
            throw;
        }
    }

    /// <summary>
    /// 内部使用的更名方法，仅由 Manager 在文件系统同步时调用.
    /// </summary>
    /// <param name="newName">新的脚本名称.</param>
    internal void UpdateNameInternal(string newName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(newName, nameof(newName));

        if (this.ScriptName == newName)
        {
            return;
        }

        try
        {
            // 开启静默
            this.isInternalSyncing = true;

            // 直接更新名称和配置管理器路径，不触发事件或执行文件操作（因为调用者已经处理了这些）
            // 是否需要区分模板模式和普通模式？对于模板模式，理论上不应该调用这个方法，因为模板的名称不应该改变
            this.isNeedReCompile = true; // 改名时需要重新编译
            this.ScriptName = newName;
            this.ScriptFilePath = this.Config.GetFilePath(newName);

            if (this.scriptFileMgr != null)
            {
                // 1. 重新实例化管理器指向新路径，但保留当前内存中的实体
                var method = this.scriptFileMgr.Method;
                this.scriptFileMgr = new ConfigurationMgr<ScriptFile>(this.ScriptFilePath, this.Script, method);

                // 2. 同步更新实体内部的名称属性
                if (this.Script != null)
                {
                    this.Script.Name = newName;
                }

                // 3. 只有在没有未保存修改时，才同步磁盘内容（防止外部改名同时改内容导致冲突）
                if (!this.IsDirty)
                {
                    var tempMgr = new ConfigurationMgr<ScriptFile>(this.ScriptFilePath, method);
                    tempMgr.LoadFromFile();

                    if (tempMgr.Entity != null && this.Script != null)
                    {
                        // 执行 Patch
                        this.Script.PatchMetadata(tempMgr.Entity);

                        // 强制确保 Name 与文件系统事件传入的 newName 严格一致
                        this.Script.Name = newName;

                        // 同步代码并卸载上下文
                        this.code = this.Script.Content;
                    }
                }
            }
        }
        finally
        {
            this.isInternalSyncing = false;
        }
    }

    /// <summary>
    /// 更新脚本代码内容（通常由编辑器调用）.
    /// </summary>
    /// <param name="newCode">新的代码字符串.</param>
    public void UpdateCode(string newCode)
    {
        if (this.code != newCode)
        {
            this.code = newCode;
            this.isNeedReCompile = true;
            this.IsDirty = true;
            this.compiler.Destory(this.input);// 代码改变，立即卸载旧程序集
        }
    }

    /// <summary>
    /// 更新编译参数.
    /// </summary>
    /// <returns>返回最新的编译参数.</returns>
    public CompilationInput UpdateCompileParameter()
    {
        return this.input.SetCode(this.code)
                 .SetAssemblyName(this.ScriptName)
                 .SetSourceCodeKind(this.Script.SourceCodeKind)
                 .SetOutputKind(OutputKind.DynamicallyLinkedLibrary)
                 .SetOptimizationLevel(OptimizationLevel.Release)
                 .ClearUsings()
                 .AddUings(ScriptFactory.DefaultUsings.ToArray())
                 .AddUings(this.Config.Usings.ToArray())
                 .AddUings(this.Script.Usings.ToArray())
                 .ClearReferences()
                 .AddReferences(ScriptFactory.DefaultReferences.ToArray())
                 .AddReferences(this.Config.References.ToArray())
                 .AddReferences(this.Script.References.ToArray());
    }

    /// <summary>
    /// 异步编译当前脚本代码.
    /// </summary>
    /// <returns>编译是否成功.</returns>
    public async Task<ScriptResult> CompileAsync()
    {
        var result = new ScriptResult(
            scriptFilePath: this.ScriptFilePath,
            scriptCode: this.Code);

        if (this.compiler == null)
        {
            return result.UpdateResult(
               success: false,
               message: "编译出错，未正常实例化编译器");
        }

        this.OnCompiling?.Invoke(this, EventArgs.Empty);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            bool isNeedReCompile = this.isNeedReCompile;

            // 如果已经有编译好的结果且未被清理，直接复用
            if (!this.isNeedReCompile)
            {
                return result.UpdateResult(
                   success: true,
                   message: LocalizationProviderFactory.GetString("脚本没有发生变化编译跳过"));
            }

            // 更新编译参数.
            this.UpdateCompileParameter();

            var compileResult = this.compiler.Compile(this.input);
            stopwatch.Stop();
            if (compileResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                this.isNeedReCompile = true;
                var compileEx = new CompilationException(compileResult.Diagnostics);
                this.compiler.Destory(this.input);
                this.OnError?.Invoke(this, new ScriptErrorEventArgs(
                    ScriptErrorSource.Compilation,
                    compileEx,
                    compileEx.Message));
                return result.UpdateResult(
                    success: false,
                    message: "编译出错：脚本语法或语义错误",
                    result: null,
                    exception: compileEx);
            }

            this.isNeedReCompile = false;
            this.OnCompiled?.Invoke(this, new ScriptCompilationEventArgs(true, compileResult.Diagnostics, stopwatch.Elapsed));
            return result.UpdateResult(
                success: true,
                message: "编译成功",
                result: null,
                exception: null);
        }
        catch (Exception ex)
        {
            this.isNeedReCompile = true;
            stopwatch.Stop();
            var message = $"框架异常：编译过程因环境或系统原因中断 - {ex.Message}";
            this.OnError?.Invoke(this, new ScriptErrorEventArgs(ScriptErrorSource.Compilation, ex, message));
            return result.UpdateResult(
                success: false,
                message: message,
                result: null,
                exception: ex);
        }
    }

    /// <summary>
    /// 异步执行已编译的脚本对象.
    /// </summary>
    /// <param name="globals">执行宿主对象.</param>
    /// <param name="ct">取消令牌.</param>
    /// <returns>脚本返回值.</returns>
    public async Task<ScriptResult> RunAsync(ScriptGlobals globals, CancellationToken ct = default)
    {
        var result = new ScriptResult(
            scriptFilePath: this.ScriptFilePath,
            scriptCode: this.Code,
            inputs: globals.Data);

        if (this.compiler == null)
        {
            return result.UpdateResult(
               success: false,
               message: "编译出错，未正常实例化编译器");
        }

        // 1. 原子性尝试获取锁。WaitAsync(0) 表示如果拿不到锁立即返回 false，不阻塞线程。
        if (!await runLock.WaitAsync(0))
        {
            return result.UpdateResult(
                success: false,
                message: "脚本正在运行中，请勿重复触发。");
        }

        var stopwatch = Stopwatch.StartNew();

        // 创建一个带超时的组合取消令牌
        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        try
        {
            this.IsRunning = true;
            this.internalCts = linkedCts;
            this.OnRunning?.Invoke(this, EventArgs.Empty);

            globals.CancellationToken = ct;
            this.input.SetScriptGlobals(globals);

            bool isReCompilations = this.isNeedReCompile;

            // TO DO 额外的判断是否需要更新的逻辑.
            if (true)
            {
            }

            if (isReCompilations)
            {
                this.UpdateCompileParameter();
            }

            var compileResult = await this.compiler.RunAsync(this.input, isReCompilations).ConfigureAwait(false);
            if (compileResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                this.isNeedReCompile = true;
                stopwatch.Stop();
                var compileEx = new CompilationException(compileResult.Diagnostics);
                this.compiler.Destory(this.input);
                this.OnError?.Invoke(this, new ScriptErrorEventArgs(
                    ScriptErrorSource.Compilation,
                    compileEx,
                    compileEx.Message));
                return result.UpdateResult(
                    success: false,
                    message: "编译出错：脚本语法或语义错误",
                    result: null,
                    exception: compileEx);
            }

            this.isNeedReCompile = false;
            var finalValue = compileResult.ReturnValue;
            globals.SetScriptResult(compileResult.ReturnValue);
            stopwatch.Stop();
            this.OnRuned?.Invoke(this, new ScriptExecutionEventArgs(finalValue, stopwatch.Elapsed));
            return result.UpdateResult(
                success: true,
                message: "执行成功",
                result: finalValue,
                exception: null,
                outputs: globals.Outputs,
                targetType: globals.ResolvedTypeName,
                targetMethod: globals.ResolvedMethodName);
        }
        catch (OperationCanceledException coeT) when (timeoutCts.IsCancellationRequested)
        {
            this.isNeedReCompile = true;
            stopwatch.Stop();
            this.OnRuned?.Invoke(this, new ScriptExecutionEventArgs(null, stopwatch.Elapsed, coeT));
            return result.UpdateResult(
                success: false,
                message: $"脚本执行超时（超过 {DefaultTimeout.TotalSeconds} 秒）",
                result: null,
                exception: coeT,
                outputs: globals.Outputs,
                targetType: globals.ResolvedTypeName,
                targetMethod: globals.ResolvedMethodName);
        }
        catch (OperationCanceledException oce)
        {
            this.isNeedReCompile = true;
            stopwatch.Stop();
            this.OnRuned?.Invoke(this, new ScriptExecutionEventArgs(null, stopwatch.Elapsed, oce));
            return result.UpdateResult(
                success: false,
                message: "执行被用户取消",
                result: null,
                exception: oce,
                outputs: globals.Outputs,
                targetType: globals.ResolvedTypeName,
                targetMethod: globals.ResolvedMethodName);
        }
        catch (Exception ex)
        {
            this.isNeedReCompile = true;
            stopwatch.Stop();

            // 解包反射调用产生的包装异常
            var actualEx = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;

            // 记录错误情况
            this.OnError?.Invoke(this, new ScriptErrorEventArgs(ScriptErrorSource.Execution, actualEx, actualEx.Message));
            this.OnRuned?.Invoke(this, new ScriptExecutionEventArgs(null, stopwatch.Elapsed, actualEx));
            return result.UpdateResult(
                success: false,
                message: $"运行异常：{actualEx.Message}",
                result: null,
                exception: actualEx,
                outputs: globals.Outputs,
                targetType: globals.ResolvedTypeName,
                targetMethod: globals.ResolvedMethodName);
        }
        finally
        {
            this.IsRunning = false;

            var cts = Interlocked.Exchange(ref this.internalCts, null);
            cts?.Dispose();

            // 必须在 finally 块中释放信号量，确保即便崩溃也不会死锁
            runLock.Release();
        }
    }

    /// <summary>
    /// 异步保存脚本.
    /// </summary>
    /// <returns>表示异步操作的任务.</returns>
    public async Task SaveAsync()
    {
        ScriptFileAction action = ScriptFileAction.Saved;

        // 校验实体是否存在
        if (this.Script == null || this.scriptFileMgr == null)
        {
            throw new InvalidOperationException("无法保存：脚本实体为空。");
        }

        // 备份实体原始状态 (用于 IO 失败时回滚实体，但编辑器 code 不回滚)
        string oldContent = this.Script.Content;
        DateTime oldTime = this.Script.ModifiedTime;

        try
        {
            // 更新待保存的内容 (更新本地副本)
            this.Script.Content = this.Code;
            this.Script.ModifiedTime = DateTime.Now;

            // 执行物理 IO 操作
            await this.scriptFileMgr.SaveToFileAsync();

            this.IsDirty = false;
            this.OnSaved?.Invoke(this, new ScriptFileEventArgs(action, this.ScriptName, this.ScriptFilePath));
        }
        catch (Exception ex)
        {
            // 失败回滚：将实体状态还原到旧值，但 this.Code 保持不变，用户可以重试
            this.Script.Content = oldContent;
            this.Script.ModifiedTime = oldTime;

            this.OnError?.Invoke(this, new ScriptErrorEventArgs(ScriptErrorSource.Saving, ex, "保存文件出错"));
            throw;
        }
    }

    /// <summary>
    /// 异步重命名当前脚本，并物理删除旧文件. 模板模式下不支持此操作.
    /// </summary>
    /// <param name="newName">新脚本名称.</param>
    /// <returns>表示异步操作的任务.</returns>
    public async Task RenameAsync(string newName)
    {
        ArgumentNullException.ThrowIfNull(newName, nameof(newName));

        // 检查 ScriptBase 和 ScriptBaseMgr 是否存在
        if (this.script == null || this.scriptFileMgr == null)
        {
            throw new InvalidOperationException("无法重命名：脚本元数据未加载.");
        }

        // 记录旧文件路径和配置管理器
        string oldFilePath = this.ScriptFilePath;
        string oldScriptName = this.ScriptName;
        var oldScriptFileMgr = this.scriptFileMgr;
        var oldIsReCompilations = this.isNeedReCompile;

        // 检查新旧名称是否一致.
        if (oldScriptName.Equals(newName, StringComparison.Ordinal))
        {
            return;
        }

        string newFilePath = this.Config.GetFilePath(newName);

        // 1. 准备新管理器（本地变量）
        var newMgr = new ConfigurationMgr<ScriptFile>(newFilePath, this.Script, this.scriptFileMgr.Method);

        try
        {
            // 更新 ScriptBase 元数据
            // 临时修改实体名称准备保存
            this.Script.Name = newName;
            this.script.ModifiedTime = DateTime.Now;

            // 执行新文件保存
            await newMgr.SaveToFileAsync();

            // 4. 全部成功后提交状态
            this.ScriptName = newName;
            this.ScriptFilePath = newFilePath;
            this.scriptFileMgr = newMgr;
            this.isNeedReCompile = true;

            // 删除旧的元数据文件
            try
            {
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }
            catch (Exception ex)
            {
                // 注意：此处不再抛出异常，因为新文件已经创建成功且状态已切换
                LogRun.Warn($"脚本已更名但旧文件删除失败: {oldFilePath}, 原因: {ex.Message}");
            }

            // 触发 Renamed 事件，并带上旧路径以便 UI 更新映射
            this.OnSaved?.Invoke(this, new ScriptFileEventArgs(
                ScriptFileAction.Renamed,
                this.ScriptName,
                newFilePath,
                oldFilePath));
        }
        catch (Exception ex)
        {
            // 回滚所有状态
            this.ScriptName = oldScriptName;
            this.Script.Name = oldScriptName;
            this.ScriptFilePath = oldFilePath;
            this.scriptFileMgr = oldScriptFileMgr;
            this.isNeedReCompile = oldIsReCompilations;

            this.OnError?.Invoke(this, new ScriptErrorEventArgs(
                ScriptErrorSource.FileOperation,
                ex,
                "重命名失败"));
        }
    }

    /// <summary>
    /// 异步将当前代码内容另存为新脚本. 原有上下文标记为已同步.
    /// </summary>
    /// <param name="newName">另存为的脚本名称.</param>
    /// <returns>返回代表新脚本文件的 <see cref="ScriptContext"/> 实例.</returns>
    public async Task<ScriptContext> SaveAsAsync(string newName)
    {
        ArgumentNullException.ThrowIfNull(newName, nameof(newName));

        if (this.script == null)
        {
            throw new InvalidOperationException("无法另存为：脚本元数据未加载.");
        }

        if (this.Script == null)
        {
            throw new InvalidOperationException("无法另存为：只能从脚本文件另存为，不支持从模板另存为.");
        }

        string oldPath = this.ScriptFilePath;
        string oldScritName = this.ScriptName;
        var oldScriptFileMgr = this.scriptFileMgr;

        string newFilePath = this.Config.GetFilePath(newName);

        // 1. 克隆实体
        ScriptFile newEntity = this.Script.Clone();
        newEntity.Name = newName;
        newEntity.Content = this.Code; // 另存为应包含当前编辑的内容
        newEntity.ModifiedTime = DateTime.Now;

        // 2. 物理保存新文件
        var newMgr = new ConfigurationMgr<ScriptFile>(newFilePath, newEntity, ConfigurationMgr<ScriptFile>.SerializeMethod.Bin);
        await newMgr.SaveToFileAsync();

        // 3. 深度同步上下文配置
        // 强制同步 Namespace 和 ReferLibs (去重处理)
        var newContext = new ScriptContext(newName, this.Config, this.compiler);
        newContext.scriptFileMgr = newMgr;
        newContext.code = this.Code;
        newContext.SubscribeMetadata(newEntity); // 订阅新实体的变化
        foreach (var ns in this.Namespaces)
        {
            newContext.Namespaces.Add(ns);
        }

        foreach (var lib in this.References)
        {
            newContext.References.Add(lib);
        }

        newContext.IsDirty = false;

        // 触发另存为事件
        this.OnSaved?.Invoke(this, new ScriptFileEventArgs(ScriptFileAction.SavedAs, newName, newFilePath, oldPath));
        return newContext;
    }

    /// <summary>
    /// 释放脚本上下文占用的资源.
    /// </summary>
    public void Dispose()
    {
        // 1. 检查运行状态
        if (this.IsRunning)
        {
            // 记录警告或尝试取消（如果存在 CancellationTokenSource）
            LogRun.Warn($"脚本 {this.ScriptName} 正在运行时被强行释放，可能引发异常。");
        }

        // 1. 原子化抢夺 CTS 控制权
        // 如果 RunAsync 还在运行，这里会拿到 cts 实例并将其成员变量置 null
        // 如果 RunAsync 已经结束，这里会拿到 null
        var cts = Interlocked.Exchange(ref this.internalCts, null);
        if (cts != null)
        {
            try
            {
                // 如果脚本正在运行，尝试发出取消信号
                cts.Cancel();
            }
            catch (ObjectDisposedException) { /* 极端情况下已被销毁，安全忽略 */ }
            catch (AggregateException) { /* 忽略取消异常 */ }
            finally
            {
                cts.Dispose();
            }
        }

        // 2. 彻底断开元数据订阅 (防止内存泄漏)
        if (this.script != null)
        {
            this.script.PropertyChanged -= OnMetadataPropertyChanged;
        }

        // 断开元数据引用，确保 ALC 能够被 GC 回收（关键！）
        this.script = null;
        this.scriptFileMgr = null;
        this.ScriptFilePath = string.Empty;

        // 4. 触发事件并注销 (防止内存泄漏)
        this.OnDispose?.Invoke(this, EventArgs.Empty);

        // 5. 清空事件订阅者
        OnDispose = null;
        OnInitialized = null;
        OnSaved = null;
        OnError = null;
        OnCompiling = null;
        OnCompiled = null;
        OnRunning = null;
        OnRuned = null;

        // 释放信号量
        runLock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 拼接并返回给定脚本名称的工作目录路径.如果目录不存在，则创建该目录.
    /// </summary>
    /// <returns>返回给定脚本名称的工作目录路径.</returns>
    public string GetWorkingDirectory()
    {
        string workspace = Path.Combine(this.Config.Directory, "Solution", this.ScriptName);
        if (!Directory.Exists(workspace))
        {
            Directory.CreateDirectory(workspace);
        }

        return workspace;
    }

    /// <summary>
    /// 拼接并返回给定脚本名称的编译输出路径.如果工作目录不存在，则创建该目录.
    /// </summary>
    /// <param name="workspace">工作目录路径.</param>
    /// <returns>返回给定脚本名称的编译路径.</returns>
    public string GetBuildPath(string? workspace = "")
    {
        if (string.IsNullOrEmpty(workspace))
        {
            workspace = Path.Combine(this.Config.Directory, this.ScriptName);
        }

        var buildPath = Path.Combine(workspace, "Src");

        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        return buildPath;
    }

    /// <summary>
    /// 拼接并返回给定引用程序集名称的完整文件路径.
    /// </summary>
    /// <param name="fileName">程序集名称.</param>
    /// <returns>返回给定引用程序集名称的完整文件路径.</returns>
    public string GetRefLibPath(string fileName)
    {
        return Path.Combine(this.ReferLibsDirectory, fileName);
    }

    /// <summary>
    /// 统一管理脚本实体的订阅与赋值.
    /// </summary>
    /// <param name="newScript">脚本文件实例对象.</param>
    private void SubscribeMetadata(ScriptFile? newScript)
    {
        // 1. 移除旧实体的事件订阅 (无论什么类型都尝试移除，确保安全)
        if (this.script != null)
        {
            this.script.PropertyChanged -= OnMetadataPropertyChanged;
        }

        // 2. 赋值
        this.script = newScript;

        // 3. 只有 ScriptFile 类型才需要监听 IsDirty
        // 模板文件 (ScriptTemplateFile) 不需要订阅
        if (this.script != null)
        {
            this.script.PropertyChanged += OnMetadataPropertyChanged;
        }
    }

    private void OnMetadataPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 如果正在内部同步，不标记为脏
        if (this.isInternalSyncing)
        {
            return;
        }

        // 排除 Content 属性，因为 Code 属性的 setter 已经处理了 IsDirty 和 UnloadContext
        // 严谨的脏检查过滤器
        bool isUserEdit = e.PropertyName switch
        {
            nameof(this.Script.Content) => false,               // Code Setter 已处理
            nameof(this.Script.ExecutionCount) => false,        // 自动统计
            nameof(this.Script.LastExecutionDuration) => false, // 自动统计
            nameof(this.Script.LastExecutionResult) => false,   // 自动统计
            nameof(this.Script.SourceCodeKind) => true,
            _ => true // 其他元数据（作者、描述、参数等）
        };

        if (isUserEdit)
        {
            if (e.PropertyName == nameof(this.Script.SourceCodeKind))
            {
                this.isNeedReCompile = true;
            }

            this.IsDirty = true;
        }
    }
}