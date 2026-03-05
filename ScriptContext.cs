using BgCommon.Configuration;
using BgCommon.Script.Models;
using DryIoc.ImTools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace BgCommon.Script;

/// <summary>
/// 脚本上下文类，集成 Roslyn 脚本引擎实现代码的加载、编译与执行.
/// </summary>
public sealed class ScriptContext : ObservableObject, IDisposable
{
    private static readonly ConcurrentDictionary<string, MetadataReference> SystemReferenceCache = new();
    private static IReadOnlyList<Assembly>? defaultBaseAssemblies;
    private static readonly IReadOnlyList<string> DefaultNamespaces = new List<string>
    {
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Text",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Diagnostics", // 解决 Debug 找不到的问题
        "System.IO",
        "BgCommon.Script",    // 允许脚本直接识别 ScriptGlobals 类型
    };

    private readonly SemaphoreSlim runLock = new(1, 1); // 初始化信号量，允许 1 个并发。
    private static int contextCount;
    private string scriptName = string.Empty;
    private string code = string.Empty;
    private string? cachedSummary;
    private string summarySourceCode = string.Empty;
    private bool isDirty;
    private bool isRunning;
    private CancellationTokenSource? internalCts;

    private Script<object>? compiledScript;
    private ScriptAssemblyLoadContext? alc;
    private InteractiveAssemblyLoader? assemblyLoader;

    // 反射元数据缓存
    private Assembly? cachedAssembly;
    private Type? cachedTargetType;
    private MethodInfo? cachedMethod;

    private ScriptBase? script;
    private ConfigurationMgr<ScriptFile>? scriptFileMgr;
    private ConfigurationMgr<ScriptTemplateFile>? templateFileMgr;
    private bool isInternalSyncing; // 标记位

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptContext"/> class.
    /// </summary>
    /// <param name="scriptName">脚本名称.</param>
    /// <param name="scriptPath">脚本存储路径.</param>
    /// <param name="referLibsPath">引用库路径.</param>
    private ScriptContext(string scriptName, string scriptPath, string referLibsPath)
    {
        // 初始化基本属性
        this.ScriptPath = scriptPath;
        this.ReferLibsPath = referLibsPath;
        this.scriptName = scriptName;
        this.Extension = "csx";
        this.Template = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptContext"/> class.
    /// </summary>
    /// <param name="name">脚本名称或模板名称.</param>
    /// <param name="config">脚本配置信息.</param>
    /// <param name="isTemplate">指示是否为脚本模板模式.</param>
    public ScriptContext(string name, ScriptConfig config, bool isTemplate)
    {
        // 验证配置对象是否为空.
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        // 统一从配置获取脚本存储根路径，确保模板模式下也能正确找到保存目录.
        this.ScriptPath = config.ScriptPath;
        this.Extension = string.IsNullOrEmpty(config.ScriptFileExtension) ? "csx" : config.ScriptFileExtension;
        this.Namespaces.AddRange(DefaultNamespaces.Concat(config.Namespaces).Distinct());
        this.ReferLibsPath = config.ReferLibsPath;
        this.ReferLibs.AddRange(config.ReferLibs);
        this.IsDirty = false;
        this.IsRunning = false;

        if (isTemplate)
        {
            // 模板模式：脚本名称初始为空.
            this.scriptName = string.Empty;
            this.Template = config.Templates.FirstOrDefault(t => t.TemplateName.Equals(name, StringComparison.OrdinalIgnoreCase));

            // 如果匹配到模板，则根据模板配置覆盖或合并引用信息.
            if (this.Template != null)
            {
                this.ReferLibsPath = this.Template.ReferLibsPath;
                if (this.Template.ReferLibs.Count > 0)
                {
                    // 若模板指定了引用库，则替换全局引用库.
                    this.ReferLibs.Clear();
                    this.ReferLibs.AddRange(this.Template.ReferLibs);
                }
            }
        }
        else
        {
            // 脚本文件模式：指定脚本名称.
            this.scriptName = name;
            this.Template = null;
        }
    }

    /// <summary>
    /// Gets 脚本运行所需的基础程序集引用.
    /// </summary>
    /// <remarks>
    /// 这些是系统级别的基础库，不随模板或配置改变而改变.
    /// </remarks>
    public IReadOnlyList<Assembly> BaseAssemblies => GetDefaultBaseAssemblies();

    /// <summary>
    /// Gets 脚本名称.
    /// </summary>
    public string ScriptName => this.scriptName;

    /// <summary>
    /// Gets or sets 脚本文件扩展名.
    /// </summary>
    public string Extension { get; set; } = "csx";

    /// <summary>
    /// Gets 脚本文件存储路径.
    /// </summary>
    public string ScriptPath { get; } = string.Empty;

    /// <summary>
    /// Gets 脚本文件的物理存储路径.
    /// </summary>
    public string ScriptFilePath => string.IsNullOrEmpty(this.ScriptName) ? string.Empty : Path.Combine(this.ScriptPath, $"{this.ScriptName}.{this.Extension}");

    /// <summary>
    /// Gets 引用程序集所在路径.
    /// </summary>
    public string ReferLibsPath { get; } = string.Empty;

    /// <summary>
    /// Gets 脚本模板配置信息.
    /// </summary>
    public ScriptTemplate? Template { get; private set; }

    /// <summary>
    /// Gets 脚本文件元数据.
    /// </summary>
    public ScriptFile? ScriptFile => this.script as ScriptFile;

    /// <summary>
    /// Gets 脚本模板文件元数据.
    /// </summary>
    public ScriptTemplateFile? TemplateFile => this.script as ScriptTemplateFile;

    /// <summary>
    /// Gets 脚本预定义的命名空间列表.
    /// </summary>
    public ObservableCollection<string> Namespaces { get; } = new ObservableCollection<string>();

    /// <summary>
    /// Gets 脚本需要引用的外部程序集列表.
    /// </summary>
    public ObservableCollection<string> ReferLibs { get; } = new ObservableCollection<string>();

    /// <summary>
    /// Gets 脚本的代码内容.
    /// </summary>
    public string Code
    {
        get => this.code;
        private set
        {
            if (this.SetProperty(ref this.code, value))
            {
                this.IsDirty = true;
                this.UnloadContext(); // 代码改变，立即卸载旧程序集
            }
        }
    }

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
                this.cachedSummary = ScriptMetadataParser.GetSummary(this.Code);
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
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

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
            // 1. 如果是模板模式，加载 ScriptTemplateFile 并从中获取初始代码.
            if (this.Template != null)
            {
                // 加载 ScriptTemplateFile 元数据
                string templateMetaFilePath = Path.Combine(this.Template.TemplatePath, $"{this.Template.TemplateName}.{this.Template.Extension}");
                this.templateFileMgr = new ConfigurationMgr<ScriptTemplateFile>(
                    templateMetaFilePath,
                    ConfigurationMgr<ScriptTemplateFile>.SerializeMethod.Bin);
                this.templateFileMgr.LoadFromFile();
                this.script = this.templateFileMgr.Entity ??
                              new ScriptTemplateFile(this.Template.TemplateName);
                if (this.TemplateFile != null && !string.IsNullOrEmpty(this.TemplateFile.Content))
                {
                    // 使用 ScriptTemplateFile 中的内容
                    this.code = this.TemplateFile.Content;
                }

                // 模板加载视为新创作，标记为已修改.
                this.IsDirty = true;
                this.OnInitialized?.Invoke(this, new ScriptFileEventArgs(ScriptFileAction.Loaded, $"Template:{this.Template.TemplateName}", this.Template.CodeTemplateFilePath));
                return;
            }

            // 2. 如果是普通脚本模式，加载 ScriptFile 并从中获取内容.
            if (!string.IsNullOrEmpty(this.ScriptFilePath))
            {
                // 加载 ScriptFile 元数据
                this.scriptFileMgr = new ConfigurationMgr<ScriptFile>(
                    this.ScriptFilePath,
                    ConfigurationMgr<ScriptFile>.SerializeMethod.Bin);
                this.scriptFileMgr.LoadFromFile();
                this.SubscribeMetadata(this.scriptFileMgr.Entity ?? new ScriptFile(this.ScriptName));
                if (this.ScriptFile != null && !string.IsNullOrEmpty(this.ScriptFile.Content))
                {
                    // 使用 ScriptFile 中的内容
                    this.code = this.ScriptFile.Content;
                    this.IsDirty = false;
                    this.OnInitialized?.Invoke(this, new ScriptFileEventArgs(
                        ScriptFileAction.Loaded,
                        this.ScriptName,
                        this.ScriptFilePath));
                }
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

        if (this.scriptName == newName)
        {
            return;
        }

        try
        {
            // 开启静默
            this.isInternalSyncing = true;

            // 直接更新名称和配置管理器路径，不触发事件或执行文件操作（因为调用者已经处理了这些）
            // 是否需要区分模板模式和普通模式？对于模板模式，理论上不应该调用这个方法，因为模板的名称不应该改变
            this.scriptName = newName;

            if (this.scriptFileMgr != null)
            {
                // 1. 重新实例化管理器指向新路径，但保留当前内存中的实体
                var method = this.scriptFileMgr.Method;
                this.scriptFileMgr = new ConfigurationMgr<ScriptFile>(this.ScriptFilePath, this.ScriptFile, method);

                // 2. 同步更新实体内部的名称属性
                if (this.ScriptFile != null)
                {
                    this.ScriptFile.Name = newName;
                }

                // 3. 只有在没有未保存修改时，才同步磁盘内容（防止外部改名同时改内容导致冲突）
                if (!this.IsDirty)
                {
                    var tempMgr = new ConfigurationMgr<ScriptFile>(this.ScriptFilePath, method);
                    tempMgr.LoadFromFile();

                    if (tempMgr.Entity != null && this.ScriptFile != null)
                    {
                        // 执行 Patch
                        this.ScriptFile.PatchMetadata(tempMgr.Entity);

                        // 强制确保 Name 与文件系统事件传入的 newName 严格一致
                        this.ScriptFile.Name = newName;

                        // 同步代码并卸载上下文
                        this.code = this.ScriptFile.Content;

                        // 必须重置编译结果，因为内容可能变了
                        this.UnloadContext();
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
            this.IsDirty = true;
            this.UnloadContext(); // 代码改变，立即卸载旧程序集
        }
    }

    /// <summary>
    /// 异步编译当前脚本代码.
    /// </summary>
    /// <returns>编译是否成功.</returns>
    public async Task<ScriptResult> CompileAsync()
    {
        // 如果已经有编译好的结果且未被清理，直接复用
        if (this.compiledScript != null)
        {
            return new ScriptResult(
               success: true,
               message: "复用已有编译结果");
        }

        this.OnCompiling?.Invoke(this, EventArgs.Empty);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 初始化新的可回收上下文
            int id = Interlocked.Increment(ref contextCount);
            this.alc = new ScriptAssemblyLoadContext($"ScriptALC_{this.ScriptName}_{id}");
            using (this.alc.EnterContextualReflection())
            {
                this.assemblyLoader = new InteractiveAssemblyLoader();
                var options = ScriptOptions.Default
                    .WithReferences(this.GetMetadataReferences())
                    .WithImports(this.Namespaces)
                    .WithOptimizationLevel(OptimizationLevel.Release);
                this.compiledScript = CSharpScript.Create<object>(
                    this.Code,
                    options,
                    typeof(ScriptGlobals),
                    this.assemblyLoader);
                var diagnostics = this.compiledScript.Compile();
                stopwatch.Stop();
                if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    var compileEx = new ScriptCompilationException(diagnostics);
                    this.UnloadContext();
                    this.OnError?.Invoke(this, new ScriptErrorEventArgs(
                        ScriptErrorSource.Compilation,
                        compileEx,
                        compileEx.Message));
                    return new ScriptResult(
                        success: false,
                        message: "编译出错：脚本语法或语义错误",
                        result: null,
                        exception: compileEx,
                        scriptFilePath: this.ScriptFilePath,
                        scriptCode: this.Code);
                }

                this.OnCompiled?.Invoke(this, new ScriptCompilationEventArgs(true, diagnostics, stopwatch.Elapsed));
                return new ScriptResult(
                    success: true,
                    message: "编译成功",
                    result: null,
                    exception: null,
                    scriptFilePath: this.ScriptFilePath,
                    scriptCode: this.Code);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.UnloadContext();
            var message = $"框架异常：编译过程因环境或系统原因中断 - {ex.Message}";
            this.OnError?.Invoke(this, new ScriptErrorEventArgs(ScriptErrorSource.Compilation, ex, message));
            return new ScriptResult(
                success: false,
                message: message,
                result: null,
                exception: ex,
                scriptFilePath: this.ScriptFilePath,
                scriptCode: this.Code);
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
        // 1. 原子性尝试获取锁。WaitAsync(0) 表示如果拿不到锁立即返回 false，不阻塞线程。
        if (!await runLock.WaitAsync(0))
        {
            return new ScriptResult(
                success: false,
                message: "脚本正在运行中，请勿重复触发。",
                scriptFilePath: this.ScriptFilePath);
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
            if (this.compiledScript == null)
            {
                // 阶段 1：自动编译（如果尚未编译）
                var compileResult = await this.CompileAsync();
                if (!compileResult.Success)
                {
                    return new ScriptResult(
                    success: compileResult.Success,
                    message: compileResult.Message,
                    result: compileResult.Result,
                    exception: compileResult.Exception,
                    inputs: globals.Data,
                    scriptFilePath: this.ScriptFilePath,
                    scriptCode: this.Code);
                }
            }

            globals.CancellationToken = ct;

            if (this.cachedAssembly == null)
            {
                // 1. 执行脚本（使用上下文引导，确保加载到当前 ALC）
                ScriptState<object>? scriptState;
                using (this.alc?.EnterContextualReflection())
                {
                    scriptState = await this.compiledScript!.RunAsync(globals, cancellationToken: this.internalCts.Token);
                }

                // 即时状态检查
                if (scriptState == null)
                {
                    throw new InvalidOperationException("引擎执行未返回状态对象");
                }

                // 检查 state.Exception 属性
                if (scriptState.Exception != null)
                {
                    // 如果顶层代码执行失败，直接抛出，不进行后续反射
                    throw scriptState.Exception;
                }

                // 如果是因为取消而停止
                ct.ThrowIfCancellationRequested();

                // 2. --- 利用 ScriptExecutionState 类获取 Assembly ---
                // 在 Roslyn 中，Submission 实例持有了生成的程序集元数据
                // 索引 0 是 globals，索引 1 是本次脚本生成的 Submission#0 对象
                var executionState = scriptState.ExecutionState;
                if (executionState != null && executionState.SubmissionStateCount >= 2)
                {
                    object submissionInstance = executionState.GetSubmissionState(1);
                    if (submissionInstance != null)
                    {
                        // 从实例反向获取 Assembly，这是最快且最准确的
                        this.cachedAssembly = submissionInstance.GetType().Assembly;
                    }
                }
                else
                {
                    // 如果直接获取失败，尝试通过 SubmissionState 的 Script 属性获取 Compilation
                    // 再从 Compilation 获取 AssemblyName，最后在当前 ALC 中匹配加载的程序集
                    if (this.cachedAssembly == null)
                    {
                        // 获取 Roslyn 为本次执行生成的程序集名称
                        var assemblyName = scriptState.Script.GetCompilation().AssemblyName;

                        // 这样可以确保拿到的 Assembly 绝对是属于当前可回收上下文的
                        this.cachedAssembly = this.alc?.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName);

                        if (this.cachedAssembly == null)
                        {
                            foreach (AssemblyLoadContext context in AssemblyLoadContext.All)
                            {
                                foreach (var assembly in context.Assemblies)
                                {
                                    if (assembly.GetName().Name == assemblyName)
                                    {
                                        this.cachedAssembly = assembly;
                                        break;
                                    }
                                }

                                if (this.cachedAssembly != null)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                if (this.cachedAssembly == null)
                {
                    throw new InvalidOperationException("无法定位生成的脚本程序集，可能是执行状态不完整。");
                }

                // 捕获 Roslyn 顶级表达式的返回值 (对应 globals.ScriptReturnValue)
                globals.SetScriptResult(scriptState.ReturnValue);

                // 解析业务入口
                this.cachedTargetType = globals.InternalResolveType(this.cachedAssembly);
                this.cachedMethod = globals.InternalResolveMethod(this.cachedTargetType);
            }

            // 此时 cachedMethod 必然不为空。
            // 直接执行反射逻辑，传入 cachedTargetType 和 cachedMethod，不再需要 state
            object? finalValue;
            using (this.alc!.EnterContextualReflection())
            {
                finalValue = await globals.ExecuteStateAsync(this.cachedTargetType, this.cachedMethod);
            }

            stopwatch.Stop();
            this.OnRuned?.Invoke(this, new ScriptExecutionEventArgs(finalValue, stopwatch.Elapsed));
            return new ScriptResult(
                success: true,
                message: "执行成功",
                result: finalValue,
                exception: null,
                inputs: globals.Data,
                outputs: globals.Outputs,
                scriptFilePath: this.ScriptFilePath,
                scriptCode: this.Code,
                targetType: globals.ResolvedTypeName,
                targetMethod: globals.ResolvedMethodName);
        }
        catch (OperationCanceledException coeT) when (timeoutCts.IsCancellationRequested)
        {
            this.OnRuned?.Invoke(this, new ScriptExecutionEventArgs(null, stopwatch.Elapsed, coeT));
            return new ScriptResult(
                success: false,
                message: $"脚本执行超时（超过 {DefaultTimeout.TotalSeconds} 秒）"
                result: null,
                exception: coeT,
                inputs: globals.Data,
                outputs: globals.Outputs,
                scriptFilePath: this.ScriptFilePath,
                scriptCode: this.Code,
                targetType: globals.ResolvedTypeName,
                targetMethod: globals.ResolvedMethodName);
        }
        catch (OperationCanceledException oce)
        {
            stopwatch.Stop();
            this.OnRuned?.Invoke(this, new ScriptExecutionEventArgs(null, stopwatch.Elapsed, oce));
            return new ScriptResult(
                success: false,
                message: "执行被用户取消",
                result: null,
                exception: oce,
                inputs: globals.Data,
                outputs: globals.Outputs,
                scriptFilePath: this.ScriptFilePath,
                scriptCode: this.Code,
                targetType: globals.ResolvedTypeName,
                targetMethod: globals.ResolvedMethodName);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // 解包反射调用产生的包装异常
            var actualEx = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;

            // 记录错误情况
            this.OnError?.Invoke(this, new ScriptErrorEventArgs(ScriptErrorSource.Execution, actualEx, actualEx.Message));
            this.OnRuned?.Invoke(this, new ScriptExecutionEventArgs(null, stopwatch.Elapsed, actualEx));
            return new ScriptResult(
                success: false,
                message: $"运行异常：{actualEx.Message}",
                result: null,
                exception: actualEx,
                inputs: globals.Data,
                outputs: globals.Outputs,
                scriptFilePath: this.ScriptFilePath,
                scriptCode: this.Code,
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
    /// 异步保存脚本. 如果当前是模板模式，则必须传入脚本名称以创建实体文件.
    /// </summary>
    /// <param name="newScriptName">模板保存为脚本时使用的名称.</param>
    /// <returns>表示异步操作的任务.</returns>
    public async Task SaveAsync(string? newScriptName = null)
    {
        // 1. 临时变量准备 (用于事务提交)
        string targetScriptName = this.scriptName;
        ScriptFile? targetEntity = this.ScriptFile;
        ConfigurationMgr<ScriptFile>? nextMgr = this.scriptFileMgr;
        ScriptFileAction action = ScriptFileAction.Saved;
        bool isFromTemplate = false;

        // 处理模板模式或未命名脚本的保存：必须指定名称.
        if (this.Template != null && string.IsNullOrEmpty(this.ScriptName))
        {
            if (string.IsNullOrWhiteSpace(newScriptName))
            {
                throw new ArgumentException("保存新脚本时必须提供脚本名称.", nameof(newScriptName));
            }

            isFromTemplate = true;
            targetScriptName = newScriptName;
            action = ScriptFileAction.CreatedFromTemplate; // 标记为从模板创建

            // 从模板创建 ScriptFile
            if (this.TemplateFile != null)
            {
                targetEntity = this.TemplateFile.CreateScriptFile(targetScriptName);
            }
            else
            {
                targetEntity = new ScriptFile(targetScriptName);
            }

            // 初始化 ScriptBase 配置管理器
            nextMgr = new ConfigurationMgr<ScriptFile>(
                Path.Combine(this.ScriptPath, $"{targetScriptName}.{this.Extension}"),
                targetEntity,
                ConfigurationMgr<ScriptFile>.SerializeMethod.Bin);
        }

        // 校验实体是否存在
        if (targetEntity == null || nextMgr == null)
        {
            throw new InvalidOperationException("无法保存：脚本实体为空。");
        }

        // 备份实体原始状态 (用于 IO 失败时回滚实体，但编辑器 code 不回滚)
        string oldContent = targetEntity.Content;
        DateTime oldTime = targetEntity.ModifiedTime;

        try
        {
            // 更新待保存的内容 (更新本地副本)
            targetEntity.Content = this.Code;
            targetEntity.ModifiedTime = DateTime.Now;

            // 执行物理 IO 操作
            await nextMgr.SaveToFileAsync();
            this.scriptName = targetScriptName;
            this.scriptFileMgr = nextMgr;

            // 使用 SubscribeMetadata 确保新创建的 ScriptFile 也能被监听
            this.SubscribeMetadata(targetEntity);

            if (isFromTemplate)
            {
                this.templateFileMgr = null; // 销毁模板管理器
                this.Template = null;        // 退出模板状态
            }

            this.IsDirty = false;
            this.OnSaved?.Invoke(this, new ScriptFileEventArgs(action, this.ScriptName, this.ScriptFilePath));
        }
        catch (Exception ex)
        {
            // 失败回滚：将实体状态还原到旧值，但 this.Code 保持不变，用户可以重试
            targetEntity.Content = oldContent;
            targetEntity.ModifiedTime = oldTime;

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

        if (this.Template != null)
        {
            throw new InvalidOperationException("从模板创建的上下文不支持重命名操作.");
        }

        // 检查 ScriptBase 和 ScriptBaseMgr 是否存在
        if (this.script == null || this.scriptFileMgr == null)
        {
            throw new InvalidOperationException("无法重命名：脚本元数据未加载.");
        }

        // 记录旧文件路径和配置管理器
        string oldFilePath = this.ScriptFilePath;
        string oldScriptName = this.ScriptName;

        // 检查新旧名称是否一致.
        if (oldScriptName.Equals(newName, StringComparison.Ordinal))
        {
            return;
        }

        var oldScriptFileMgr = this.scriptFileMgr;
        var oldTemplateFileMgr = this.templateFileMgr;
        string newFilePath = Path.Combine(this.ScriptPath, $"{newName}.{this.Extension}");

        // 1. 准备新管理器（本地变量）
        var newMgr = new ConfigurationMgr<ScriptFile>(newFilePath, this.ScriptFile, this.scriptFileMgr.Method);

        try
        {
            // 更新 ScriptBase 元数据
            // 临时修改实体名称准备保存
            this.script.Name = newName;
            this.script.ModifiedTime = DateTime.Now;

            // 执行新文件保存
            await newMgr.SaveToFileAsync();

            // 4. 全部成功后提交状态
            this.scriptName = newName;
            this.scriptFileMgr = newMgr;

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
            this.scriptName = oldScriptName;
            this.script.Name = oldScriptName;
            this.scriptFileMgr = oldScriptFileMgr;
            this.templateFileMgr = oldTemplateFileMgr;

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

        if (this.ScriptFile == null)
        {
            throw new InvalidOperationException("无法另存为：只能从脚本文件另存为，不支持从模板另存为.");
        }

        string oldPath = this.ScriptFilePath;
        string targetPath = Path.Combine(this.ScriptPath, $"{newName}.{this.Extension}");

        // 1. 克隆实体
        ScriptFile newEntity = this.ScriptFile.Clone();
        newEntity.Name = newName;
        newEntity.Content = this.Code; // 另存为应包含当前编辑的内容
        newEntity.ModifiedTime = DateTime.Now;

        // 2. 物理保存新文件
        var newMgr = new ConfigurationMgr<ScriptFile>(targetPath, newEntity, ConfigurationMgr<ScriptFile>.SerializeMethod.Bin);
        await newMgr.SaveToFileAsync();

        // 3. 深度同步上下文配置
        // 强制同步 Namespace 和 ReferLibs (去重处理)
        var newContext = new ScriptContext(newName, this.ScriptPath, this.ReferLibsPath);
        newContext.Extension = this.Extension;
        newContext.code = this.Code;
        newContext.scriptFileMgr = newMgr;
        newContext.SubscribeMetadata(newEntity); // 订阅新实体的变化
        foreach (var ns in this.Namespaces)
        {
            newContext.Namespaces.Add(ns);
        }

        foreach (var lib in this.ReferLibs)
        {
            newContext.ReferLibs.Add(lib);
        }

        newContext.IsDirty = false;

        // 触发另存为事件
        this.OnSaved?.Invoke(this, new ScriptFileEventArgs(ScriptFileAction.SavedAs, newName, targetPath, oldPath));
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
        this.templateFileMgr = null;

        // 3. 彻底清理 Roslyn 资源和元数据缓存
        this.UnloadContext();

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
    /// 获取当前脚本环境下所有引用的元数据引用 (MetadataReference).
    /// 常用于初始化 RoslynPad 编辑器.
    /// </summary>
    /// <returns>程序集引用集合.</returns>
    public IEnumerable<MetadataReference> GetMetadataReferencesForEditor()
    {
        // foreach (var assembly in GetMetadataReferences())
        // {
        //     if (!string.IsNullOrEmpty(assembly.Location))
        //     {
        //         yield return MetadataReference.CreateFromFile(assembly.Location);
        //     }
        // }
        return GetMetadataReferences().ToArray();
    }

    /// <summary>
    /// 解析并获取所有元数据引用.
    /// </summary>
    /// <returns>程序集引用集合.</returns>
    private IEnumerable<MetadataReference> GetMetadataReferences()
    {
        // 1. 首先放入基础核心程序集
        var metadataReferences = this.BaseAssemblies.Select(asm =>
            SystemReferenceCache.GetOrAdd(
                asm.Location,
                loc => MetadataReference.CreateFromFile(loc)))
            .ToList();

        // 2. 加载用户/模板配置的引用库
        foreach (string libName in this.ReferLibs)
        {
            try
            {
                string fullPath = Path.Combine(this.ReferLibsPath, libName);
                if (File.Exists(fullPath))
                {
                    metadataReferences.Add(MetadataReference.CreateFromFile(fullPath));
                }
            }
            catch (Exception ex)
            {
                LogRun.Error($"无法加载程序集引用: {libName}, {ex.Message}");
            }
        }

        // 4. 去重并返回
        return metadataReferences.Distinct();
    }

    /// <summary>
    /// 清理并卸载旧的动态程序集上下文.
    /// </summary>
    private void UnloadContext()
    {
        // 断开元数据引用，确保 ALC 能够被 GC 回收（关键！）
        this.cachedSummary = null;
        this.compiledScript = null;
        this.cachedAssembly = null;
        this.cachedTargetType = null;
        this.cachedMethod = null;

        // 移除 loader 对 ALC 的引用
        if (this.assemblyLoader != null)
        {
            this.assemblyLoader.Dispose();
            this.assemblyLoader = null;
        }

        if (this.alc != null)
        {
            try
            {
                // 显式卸载 ALC
                this.alc.Unload();
                LogRun.Info($"脚本上下文 {this.ScriptName} 的旧程序集已请求卸载.");
            }
            catch (Exception ex)
            {
                LogRun.Error($"卸载脚本程序集上下文时出错: {ex.Message}");
            }
            finally
            {
                this.alc = null;
            }
        }
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
            nameof(ScriptBase.Content) => false,                // Code Setter 已处理
            nameof(ScriptFile.ExecutionCount) => false,        // 自动统计
            nameof(ScriptFile.LastExecutionDuration) => false, // 自动统计
            nameof(ScriptFile.LastExecutionResult) => false,   // 自动统计
            _ => true // 其他元数据（作者、描述、参数等）
        };

        if (isUserEdit)
        {
            this.IsDirty = true;
        }
    }

    private static IReadOnlyList<Assembly> GetDefaultBaseAssemblies()
    {
        if (defaultBaseAssemblies != null)
        {
            return defaultBaseAssemblies;
        }

        var assemblies = new List<Assembly>
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Enumerable).Assembly,
            typeof(System.Diagnostics.Debug).Assembly,       // 显式包含 Debug 所在程序集
            typeof(System.ComponentModel.Component).Assembly,
            typeof(System.Text.Json.JsonSerializer).Assembly,
            Assembly.Load("System.Runtime"),
            Assembly.Load("netstandard"),
            Assembly.GetExecutingAssembly(),
        };

        // 尝试加载可选的 RoslynPad 支持
        try
        {
            assemblies.Add(Assembly.Load("RoslynPad.Runtime"));
        }
        catch (Exception ex)
        {
            // 如果找不到，忽略即可，不影响核心功能
            LogRun.Warn("未检测到 RoslynPad.Runtime，相关增强功能将不可用。");
        }

        defaultBaseAssemblies = assemblies;
        return defaultBaseAssemblies;
    }
}