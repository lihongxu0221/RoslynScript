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

/// <summary>
/// 脚本上下文类，集成 Roslyn 脚本引擎实现代码的加载、编译与执行.
/// </summary>
public sealed class ScriptContext : IDisposable
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

    private static int contextCount;
    private string scriptName = string.Empty;
    private string code = string.Empty;
    private string? cachedSummary;
    private string summarySourceCode = string.Empty;
    private bool isDirty;
    private bool isRunning;

    private Script<object>? compiledScript;
    private ScriptAssemblyLoadContext? alc;
    private InteractiveAssemblyLoader? assemblyLoader;

    // 反射元数据缓存
    private Assembly? cachedAssembly;
    private Type? cachedTargetType;
    private MethodInfo? cachedMethod;

    private ScriptBase? scriptBase;
    private ConfigurationMgr<ScriptBase>? scriptBaseMgr;

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
        this.Namespaces = DefaultNamespaces.Concat(config.Namespaces).Distinct().ToList();
        this.ReferLibsPath = config.ReferLibsPath;
        this.ReferLibs = new List<string>(config.ReferLibs);
        this.isDirty = false;
        this.isRunning = false;

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
    public ScriptFile? ScriptFile => this.scriptBase as ScriptFile;

    /// <summary>
    /// Gets 脚本模板文件元数据.
    /// </summary>
    public ScriptTemplateFile? TemplateFile => this.scriptBase as ScriptTemplateFile;

    /// <summary>
    /// Gets 脚本预定义的命名空间列表.
    /// </summary>
    public List<string> Namespaces { get; } = new List<string>();

    /// <summary>
    /// Gets 脚本需要引用的外部程序集列表.
    /// </summary>
    public List<string> ReferLibs { get; } = new List<string>();

    /// <summary>
    /// Gets 脚本的代码内容.
    /// </summary>
    public string Code
    {
        get => this.code;
        private set
        {
            if (this.code != value)
            {
                this.code = value;
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
            if (cachedSummary == null || summarySourceCode != this.Code)
            {
                summarySourceCode = this.Code;
                cachedSummary = ScriptMetadataParser.GetSummary(this.Code);
            }

            return cachedSummary;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether 脚本内容是否已更改且未保存.
    /// </summary>
    public bool IsDirty
    {
        get => this.isDirty;
        set => this.isDirty = value;
    }

    /// <summary>
    /// Gets a value indicating whether 脚本是否正在运行.
    /// </summary>
    public bool IsRunning
    {
        get => this.isRunning;
        private set => this.isRunning = value;
    }

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
                this.scriptBaseMgr = new ConfigurationMgr<ScriptBase>(templateMetaFilePath, ConfigurationMgr<ScriptBase>.SerializeMethod.Bin);
                this.scriptBaseMgr.LoadFromFile();
                this.scriptBase = this.scriptBaseMgr.Entity ?? new ScriptTemplateFile(this.Template.TemplateName);

                var templateFile = this.scriptBase as ScriptTemplateFile;
                if (templateFile != null && !string.IsNullOrEmpty(templateFile.Content))
                {
                    // 使用 ScriptTemplateFile 中的内容
                    this.code = templateFile.Content;
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
                this.scriptBaseMgr = new ConfigurationMgr<ScriptBase>(this.ScriptFilePath, ConfigurationMgr<ScriptBase>.SerializeMethod.Bin);
                this.scriptBaseMgr.LoadFromFile();
                this.scriptBase = this.scriptBaseMgr.Entity ?? new ScriptFile(this.ScriptName);

                var scriptFile = this.scriptBase as ScriptFile;
                if (scriptFile != null && !string.IsNullOrEmpty(scriptFile.Content))
                {
                    // 使用 ScriptFile 中的内容
                    this.code = scriptFile.Content;
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
        this.scriptName = newName;
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
        if (this.compiledScript == null)
        {
            // 阶段 1：自动编译（如果尚未编译）
            if (this.compiledScript == null)
            {
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
        }

        this.IsRunning = true;
        this.OnRunning?.Invoke(this, EventArgs.Empty);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            globals.CancellationToken = ct;

            if (this.cachedAssembly == null)
            {
                // 1. 执行脚本（使用上下文引导，确保加载到当前 ALC）
                ScriptState<object>? scriptState;
                using (this.alc?.EnterContextualReflection())
                {
                    scriptState = await this.compiledScript!.RunAsync(globals, cancellationToken: ct);
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
        }
    }

    /// <summary>
    /// 异步保存脚本. 如果当前是模板模式，则必须传入脚本名称以创建实体文件.
    /// </summary>
    /// <param name="newScriptName">模板保存为脚本时使用的名称.</param>
    /// <returns>表示异步操作的任务.</returns>
    public async Task SaveAsync(string? newScriptName = null)
    {
        try
        {
            ScriptFileAction action = ScriptFileAction.Saved;

            // 处理模板模式或未命名脚本的保存：必须指定名称.
            if (this.Template != null && string.IsNullOrEmpty(this.ScriptName))
            {
                if (string.IsNullOrWhiteSpace(newScriptName))
                {
                    throw new ArgumentException("保存新脚本时必须提供脚本名称.", nameof(newScriptName));
                }

                this.scriptName = newScriptName;
                action = ScriptFileAction.CreatedFromTemplate; // 标记为从模板创建

                // 从模板创建 ScriptFile
                var templateFile = this.scriptBase as ScriptTemplateFile;
                if (templateFile != null)
                {
                    this.scriptBase = templateFile.CreateScriptFile(newScriptName);
                }
                else
                {
                    this.scriptBase = new ScriptFile(newScriptName);
                }

                // 初始化 ScriptBase 配置管理器
                string scriptFilePath = Path.Combine(this.ScriptPath, $"{newScriptName}.{this.Extension}");
                this.scriptBaseMgr = new ConfigurationMgr<ScriptBase>(scriptFilePath, this.scriptBase, ConfigurationMgr<ScriptBase>.SerializeMethod.Bin);
            }

            if (string.IsNullOrEmpty(this.ScriptFilePath))
            {
                throw new InvalidOperationException("保存路径无效，文件名不能为空.");
            }

            // 更新 ScriptBase 的内容
            if (this.scriptBase != null)
            {
                this.scriptBase.Content = this.Code;
                this.scriptBase.ModifiedTime = DateTime.Now;

                // 保存 ScriptBase 元数据
                if (this.scriptBaseMgr != null)
                {
                    await this.scriptBaseMgr.SaveToFileAsync();
                }
            }

            this.IsDirty = false;
            this.OnSaved?.Invoke(this, new ScriptFileEventArgs(action, this.ScriptName, this.ScriptFilePath));

            if (this.Template != null)
            {
                this.Template = null;
            }
        }
        catch (Exception ex)
        {
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
        if (this.scriptBase == null || this.scriptBaseMgr == null)
        {
            throw new InvalidOperationException("无法重命名：脚本元数据未加载.");
        }

        // 记录旧文件路径和配置管理器
        string oldFilePath = this.ScriptFilePath;
        string oldScriptName = this.ScriptName;
        var oldScriptBaseMgr = this.scriptBaseMgr;

        // 检查新旧名称是否一致.
        if (oldScriptName.Equals(newName, StringComparison.Ordinal))
        {
            return;
        }

        this.scriptName = newName;
        string newFilePath = this.ScriptFilePath;

        try
        {
            // 更新 ScriptBase 元数据
            this.scriptBase.Name = newName;
            this.scriptBase.ModifiedTime = DateTime.Now;

            // 创建新的配置管理器并保存到新路径（原子性操作）
            var newScriptBaseMgr = new ConfigurationMgr<ScriptBase>(newFilePath, this.scriptBase, ConfigurationMgr<ScriptBase>.SerializeMethod.Bin);
            await newScriptBaseMgr.SaveToFileAsync();

            // 新文件保存成功后，更新配置管理器引用
            this.scriptBaseMgr = newScriptBaseMgr;

            // 删除旧的元数据文件
            if (File.Exists(oldFilePath))
            {
                await Task.Run(() => File.Delete(oldFilePath));
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
            this.scriptBase.Name = oldScriptName;
            this.scriptBaseMgr = oldScriptBaseMgr;

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

        if (this.Template != null)
        {
            throw new InvalidOperationException("从模板创建的上下文不支持另存为操作.");
        }

        string oldPath = this.ScriptFilePath;
        string targetPath = Path.Combine(this.ScriptPath, $"{newName}.{this.Extension}");

        // 另存为成功后，当前内存中的代码已视为已同步到磁盘副本.
        this.IsDirty = false;

        // 检查 ScriptBase 是否存在
        if (this.scriptBase == null)
        {
            throw new InvalidOperationException("无法另存为：脚本元数据未加载.");
        }

        // 创建新的 ScriptFile（只支持从 ScriptFile 另存为）
        var currentScriptFile = this.scriptBase as ScriptFile;
        if (currentScriptFile == null)
        {
            throw new InvalidOperationException("无法另存为：只能从脚本文件另存为，不支持从模板另存为.");
        }

        // 克隆当前的 ScriptFile
        ScriptBase newScriptBase = currentScriptFile.Clone();
        newScriptBase.Name = newName;
        newScriptBase.ModifiedTime = DateTime.Now;

        // 保存新的 ScriptFile 元数据
        var newScriptBaseMgr = new ConfigurationMgr<ScriptBase>(targetPath, newScriptBase, ConfigurationMgr<ScriptBase>.SerializeMethod.Bin);
        await newScriptBaseMgr.SaveToFileAsync();

        // 初始化并返回一个全新的上下文对象.
        var newContext = new ScriptContext(newName, this.ScriptPath, this.ReferLibsPath);
        newContext.Namespaces.AddRange(this.Namespaces);
        newContext.ReferLibs.AddRange(this.ReferLibs);
        newContext.Extension = this.Extension;
        newContext.code = this.Code;
        newContext.scriptBase = newScriptBase;
        newContext.scriptBaseMgr = newScriptBaseMgr;
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
        // 1. 显式卸载程序集上下文
        this.UnloadContext();

        // 2. 触发事件
        this.OnDispose?.Invoke(this, EventArgs.Empty);

        // 3. 压制 GC
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