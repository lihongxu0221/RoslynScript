using BgCommon.Script.Runtime.CodeAnalysis;
using BgCommon.Script.Runtime.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace BgCommon.Script.Runtime.Compilation.CSharp;

/// <summary>
/// CSharp 代码编译器实现类.
/// </summary>
internal class CSharpCodeCompiler : ICodeCompiler
{
    /// <summary>
    /// 缓存已加载的源生成器，避免重复执行反射和文件 IO.
    /// </summary>
    private static readonly ConcurrentDictionary<string, ISourceGenerator[]> GeneratorCache = new();

    /// <summary>
    /// 缓存编译结果，键为输入参数的哈希值.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, CompilationResult> CompilationCache = new();

    /// <summary>
    /// 缓存已加载的程序集及其上下文，实现驻留复用.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, AssemblyLoadContext> ActiveContexts = new();

    /// <summary>
    /// 缓存每个文档已经加载到内存中的程序集对象.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, Assembly> AssemblyCache = new();

    /// <summary>
    /// .NET 运行时信息服务.
    /// </summary>
    private readonly IDotNetInfo dotNetInfo;

    /// <summary>
    /// 代码分析服务.
    /// </summary>
    private readonly ICodeAnalysisService codeAnalysisService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpCodeCompiler"/> class.
    /// </summary>
    /// <param name="dotNetInformation">.NET 信息实例.</param>
    /// <param name="analysisService">代码分析服务实例Index.</param>
    public CSharpCodeCompiler(
        IDotNetInfo dotNetInformation,
        ICodeAnalysisService analysisService)
    {
        this.dotNetInfo = dotNetInformation;
        this.codeAnalysisService = analysisService;
    }

    /// <summary>
    /// 执行代码编译并返回编译结果.
    /// </summary>
    /// <param name="input">编译输入参数.</param>
    /// <returns>编译结果信息.</returns>
    public CompilationResult Compile(CompilationInput input)
    {
        // 验证输入参数.
        ArgumentNullException.ThrowIfNull(input, nameof(input));

        // 确定程序集名称.
        string assemblyName = !string.IsNullOrWhiteSpace(input.AssemblyName) ?
            input.AssemblyName :
            "BaiguScript";

        // 获取编译程序集的后缀名.
        string fileExtension = this.GetCompiledAssemblyFileExtension(input.OutputKind);
        string assemblyFullName = $"{assemblyName}{fileExtension}";

        // 创建 CSharp 编译对象.
        var compilation = this.CreateCompilation(input, assemblyName);

        // 使用内存流存储编译生成的字节码.
        using var outputStream = new MemoryStream();
        var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);

        // 执行编译发射，并嵌入调试信息.
        var emitResult = compilation.Emit(
            peStream: outputStream,
            options: emitOptions);

        // 将流指针移至起始位置并转换为字节数组.
        outputStream.Seek(0, SeekOrigin.Begin);
        var assemblyData = outputStream.ToArray();

        return new CompilationResult(
            emitResult.Success,
            new AssemblyName(assemblyName),
            assemblyFullName,
            assemblyData,
            emitResult.Diagnostics);
    }

    /// <summary>
    /// 编译并运行脚本，执行完成后自动卸载程序集上下文.
    /// </summary>
    /// <param name="input">编译输入参数.</param>
    /// <param name="recompile">是否强制重新编译. 如果为 false 且缓存中存在，则使用旧程序集.</param>
    /// <param name="unloadAfterExecution">执行完成后是否立即卸载上下文并清理内存.</param>
    /// <returns>编译结果信息.</returns>
    public async Task<CompilationResult> RunAsync(
        CompilationInput input,
        bool recompile = true,
        bool unloadAfterExecution = false)
    {
        // 验证参数有效性.
        ArgumentNullException.ThrowIfNull(input, nameof(input));

        // 1. 判断是否需要重新编译并清理旧上下文.
        if (recompile)
        {
            this.CleanupContext(input.Id);
        }

        // 2. 获取或创建编译结果.
        if (!CompilationCache.TryGetValue(input.Id, out var compilationResult))
        {
            compilationResult = this.Compile(input);
            if (compilationResult.Success)
            {
                CompilationCache[input.Id] = compilationResult;
            }
        }

        // 3. 执行脚本逻辑.
        if (compilationResult.Success && compilationResult.AssemblyData != null)
        {
            var returnValue = await this.ExecuteInternalAsync(input, compilationResult.AssemblyData, unloadAfterExecution);
            compilationResult.WithReturnValue(returnValue);

            // 4. 如果要求执行后立即清理.
            if (unloadAfterExecution)
            {
                this.CleanupContext(input.Id);

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        return compilationResult;
    }

    /// <summary>
    /// 从上下文中销毁指定脚本编译结果.
    /// </summary>
    /// <param name="input">编译输入信息.</param>
    public void Destory(CompilationInput input)
    {
        this.CleanupContext(input.Id);
    }

    /// <summary>
    /// 在隔离的上下文中执行脚本逻辑.
    /// </summary>
    /// <param name="input">编译输入参数.</param>
    /// <param name="assemblyData">程序集二进制数据.</param>
    /// <param name="isCollectible">是否可回收.</param>
    /// <returns>执行返回值.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<object?> ExecuteInternalAsync(
        CompilationInput input,
        byte[] assemblyData,
        bool isCollectible)
    {
        ScriptGlobals scriptGlobals = input.ScriptGlobals;

        // 尝试从缓存中直接获取已加载的程序集.
        if (!AssemblyCache.TryGetValue(input.Id, out var scriptAssembly))
        {
            // 确保加载上下文存在.
            if (!ActiveContexts.TryGetValue(input.Id, out var loadContext))
            {
                string contextName = $"ScriptContext_{input.Id:N}";
                loadContext = new HostSharingLoadContext(contextName);
                ActiveContexts[input.Id] = loadContext;
            }

            // 加载程序集并存入缓存.
            using var assemblyStream = new MemoryStream(assemblyData);
            scriptAssembly = loadContext.LoadFromStream(assemblyStream);
            AssemblyCache[input.Id] = scriptAssembly;
        }

        try
        {
            // 解析类型与方法.
            var targetType = scriptGlobals.InternalResolveType(scriptAssembly);
            var targetMethod = scriptGlobals.InternalResolveMethod(targetType);

            // 执行脚本逻辑（同步等待异步执行结果）.
            return await scriptGlobals.ExecuteStateAsync(targetType, targetMethod);
        }
        catch (TargetInvocationException invocationEx)
        {
            // 处理反射调用内部抛出的异常.
            throw invocationEx.InnerException ?? invocationEx;
        }
        catch (Exception)
        {
            // 重新抛出其他运行期异常，确保外层调用者感知，同时保证 finally 块能执行卸载逻辑.
            throw;
        }
    }

    /// <summary>
    /// 清理指定文档关联的上下文和缓存.
    /// </summary>
    /// <param name="id">文档 ID.</param>
    private void CleanupContext(Guid id)
    {
        if (ActiveContexts.TryRemove(id, out var oldContext))
        {
            oldContext.Unload();
        }

        // 2. 清理程序集缓存.
        AssemblyCache.TryRemove(id, out _);

        // 3. 清理编译结果缓存.
        CompilationCache.TryRemove(id, out _);
    }

    /// <summary>
    /// 根据输入信息创建并配置 CSharp 编译实例.
    /// </summary>
    /// <param name="input">编译输入配置.</param>
    /// <param name="assemblyName">程序集名称.</param>
    /// <returns>配置完成的 CSharpCompilation 对象.</returns>
    private CSharpCompilation CreateCompilation(CompilationInput input, string assemblyName)
    {
        // 获取源代码语法树（包含预制的 using 语句以支持智能提示）
        var syntaxTree = this.codeAnalysisService.GetSyntaxTree(
            input.Code,
            input.SourceCodeKind,
            input.TargetFrameworkVersion,
            input.OptimizationLevel,
            input.Usings);

        // 使用字典管理引用，键为程序集简单名称，防止 CS1703 标识冲突.
        var referenceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. 加载 SDK 基础程序集.
        var sdkPaths = FrameworkAssemblies.GetAssemblyLocations(
            this.dotNetInfo.LocateDotNetRootDirectoryOrThrow(),
            input.TargetFrameworkVersion,
            input.UseAspNet);
        this.AddToReferenceMap(referenceMap, sdkPaths);

        // 2. 加载当前宿主上下文引用（处理单文件发布兼容性）.
        var hostPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => a.Location);
        this.AddToReferenceMap(referenceMap, hostPaths);

        // 3. 显式确保 ScriptGlobals 及其具体实现的程序集被引用
        var globalsAssembly = input.ScriptGlobals.GetType().Assembly;
        var globalsPath = globalsAssembly.Location;
        if (!string.IsNullOrEmpty(globalsPath))
        {
            referenceMap[Path.GetFileNameWithoutExtension(globalsPath)] = globalsPath;
        }

        var globalsNamespace = input.ScriptGlobals.GetType().Namespace;
        if (!string.IsNullOrEmpty(globalsNamespace))
        {
            input.AddUings(globalsNamespace);
        }

        // 构建元数据引用集合.
        var metadataReferences = this.BuildMetadataReferences(
            referenceMap.Values.ToHashSet(),
            input.ImageReferences,
            input.References);

        // 编译选项配置.
        var options = new CSharpCompilationOptions(input.OutputKind)
            .WithUsings(input.Usings.Distinct())
            .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
            .WithOptimizationLevel(input.OptimizationLevel)
            .WithOverflowChecks(true)
            .WithAllowUnsafe(true);

        // 创建初始编译对象.
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new SyntaxTree[] { syntaxTree },
            options: options,
            references: metadataReferences);

        // 使用语法树重写器添加 using 语句
        if (input.Usings.Count > 0)
        {
            compilation = this.AddUsingsToCompilation(compilation, input.Usings);
        }

        // 获取并运行源代码生成器（Source Generators）.
        var generators = GetSourceGenerators(referenceMap.Values.ToHashSet());
        var driver = CSharpGeneratorDriver.Create(
            generators: generators,
            additionalTexts: null,
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees[0].Options,
            optionsProvider: null);

        // 执行生成器并更新编译对象.
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (CSharpCompilation)output;
    }

    /// <summary>
    /// 将路径集合添加到程序集引用字典中，实现简单去重逻辑.
    /// </summary>
    /// <param name="map">程序集引用字典.</param>
    /// <param name="paths">程序集路径集合.</param>
    private void AddToReferenceMap(Dictionary<string, string> map, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(fileName))
            {
                // 如果已存在相同名称的程序集，此处策略可根据需要调整（如版本比对），目前简单保留最新扫描到的路径.
                map[fileName] = path;
            }
        }
    }

    /// <summary>
    /// 根据输出类型获取编译程序集的文件后缀名.
    /// </summary>
    /// <param name="outputKind">输出类型.</param>
    /// <returns>后缀名字符串.</returns>
    private string GetCompiledAssemblyFileExtension(OutputKind outputKind)
    {
        // 获取当前平台的执行文件后缀.
        var platformExt = PlatformUtil.GetPlatformExecutableExtension();

        return outputKind switch
        {
            OutputKind.DynamicallyLinkedLibrary => ".dll",
            OutputKind.ConsoleApplication => platformExt,
            OutputKind.WindowsApplication => platformExt,
            OutputKind.WindowsRuntimeMetadata => platformExt,
            OutputKind.WindowsRuntimeApplication => ".winmdobj",
            OutputKind.NetModule => ".netmodule",
            _ => throw new ArgumentOutOfRangeException(nameof(outputKind), outputKind, null)
        };
    }

    /// <summary>
    /// 将程序集镜像和文件路径转换为元数据引用.
    /// </summary>
    /// <param name="locations">文件路径集合Index.</param>
    /// <param name="images">字节数组镜像集合.</param>
    /// <param name="references">引用文件路径.</param>
    /// <returns>可移植可执行引用数组.</returns>
    private MetadataReference[] BuildMetadataReferences(
        HashSet<string> locations,
        IEnumerable<byte[]> images,
        HashSet<LibraryRef> references)
    {
        // 合并镜像引用和文件路径引用.
        var fileRefs = locations.Select(loc => MetadataReference.CreateFromFile(loc));
        var imageRefs = images.Select(img => MetadataReference.CreateFromImage(img));
        var libraryRefs = references.Select(r => r.Resolve());

        return imageRefs.Union(fileRefs).Union(libraryRefs).ToArray();
    }

    /// <summary>
    /// 获取源生成器集合，并利用静态字典进行缓存.
    /// </summary>
    /// <param name="locations">程序集位置集合.</param>
    /// <returns>源生成器数组.</returns>
    private ISourceGenerator[] GetSourceGenerators(HashSet<string> locations)
    {
        var loader = new FromAssemblyLoader();
        var results = new List<ISourceGenerator>();

        foreach (var path in locations)
        {
            var fileName = Path.GetFileName(path);

            // 排除已知的系统核心库，显著减少无效的反射加载开销.
            if (fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var generators = GeneratorCache.GetOrAdd(path, key =>
            {
                try
                {
                    var analyzerRef = new AnalyzerFileReference(key, loader);
                    return analyzerRef.GetGenerators("C#").ToArray();
                }
                catch
                {
                    // 若加载失败，返回空数组防止重复尝试.
                    return Array.Empty<ISourceGenerator>();
                }
            });

            results.AddRange(generators);
        }

        return results.ToArray();
    }

    /// <summary>
    /// 使用语法树重写器为编译对象添加 using 语句
    /// </summary>
    /// <param name="compilation">原始编译对象</param>
    /// <param name="usings">需要添加的命名空间集合</param>
    /// <returns>添加了 using 语句的编译对象</returns>
    private CSharpCompilation AddUsingsToCompilation(CSharpCompilation compilation, HashSet<string> usings)
    {
        if (usings == null || usings.Count == 0)
        {
            return compilation;
        }

        // 过滤掉空的命名空间
        var validUsings = usings.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        if (validUsings.Count == 0)
        {
            return compilation;
        }

        // 获取第一个语法树（假设只有一个语法树）
        var originalTree = compilation.SyntaxTrees.FirstOrDefault();
        if (originalTree == null)
        {
            return compilation;
        }

        // 获取根节点
        var root = originalTree.GetRoot();
        
        // 检查是否已经有 using 语句
        var existingUsings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
        foreach (var usingDirective in usingDirectives)
        {
            var name = usingDirective.Name?.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                existingUsings.Add(name);
            }
        }

        // 找出需要添加的 using 语句
        var usingsToAdd = validUsings.Where(u => !existingUsings.Contains(u.Trim())).ToList();
        if (usingsToAdd.Count == 0)
        {
            return compilation;
        }

        // 创建新的 using 语句
        var newUsings = usingsToAdd.Select(u => 
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u.Trim()))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
        ).ToArray();

        // 创建语法树重写器
        var rewriter = new UsingAdderRewriter(newUsings);
        var newRoot = rewriter.Visit(root);

        // 如果根节点有变化，创建新的语法树
        if (newRoot != root)
        {
            var newTree = originalTree.WithRootAndOptions(newRoot, originalTree.Options);
            return compilation.ReplaceSyntaxTree(originalTree, newTree);
        }

        return compilation;
    }

    /// <summary>
    /// 语法树重写器，用于添加 using 语句
    /// </summary>
    private class UsingAdderRewriter : CSharpSyntaxRewriter
    {
        private readonly UsingDirectiveSyntax[] newUsings;
        private bool hasAddedUsings = false;

        public UsingAdderRewriter(UsingDirectiveSyntax[] newUsings)
        {
            this.newUsings = newUsings;
        }

        public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
        {
            if (hasAddedUsings)
            {
                return base.VisitCompilationUnit(node);
            }

            hasAddedUsings = true;
            
            // 在现有 using 语句之后添加新的 using 语句
            var existingUsings = node.Usings;
            var allUsings = existingUsings.AddRange(newUsings);
            
            return node.WithUsings(allUsings);
        }

        public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            if (hasAddedUsings)
            {
                return base.VisitNamespaceDeclaration(node);
            }

            hasAddedUsings = true;
            
            // 在命名空间内的 using 语句之后添加新的 using 语句
            var existingUsings = node.Usings;
            var allUsings = existingUsings.AddRange(newUsings);
            
            return node.WithUsings(allUsings);
        }
    }
}

/// <summary>
/// 支持与宿主程序共享引用上下文的可回收程序集加载上下文.
/// </summary>
file class HostSharingLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostSharingLoadContext"/> class.
    /// </summary>
    /// <param name="name">上下文名称.</param>
    public HostSharingLoadContext(string name)
        : base(name, isCollectible: true)
    {
    }

    /// <summary>
    /// 在加载程序集时执行解析逻辑.
    /// </summary>
    /// <param name="assemblyName">待解析的程序集名称.</param>
    /// <returns>解析后的程序集对象.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 关键逻辑：优先尝试从 Default 上下文（主程序所在上下文）加载程序集
        // 这样可以确保脚本使用的接口、基类和宿主程序完全一致
        try
        {
            var hostAssembly = Default.LoadFromAssemblyName(assemblyName);
            if (hostAssembly != null)
            {
                return hostAssembly;
            }
        }
        catch
        {
            // 忽略加载失败，继续尝试默认逻辑
        }

        return null;
    }
}

/// <summary>
/// 从程序集路径加载分析器的加载器.
/// </summary>
file class FromAssemblyLoader : IAnalyzerAssemblyLoader
{
    /// <summary>
    /// 存储已加载程序集的并发字典缓存.
    /// </summary>
    private readonly ConcurrentDictionary<string, Assembly> loadedAssemblies = new();

    /// <summary>
    /// 添加程序集依赖项的搜索路径.
    /// </summary>
    /// <param name="fullPath">依赖项的完整路径.</param>
    public void AddDependencyLocation(string fullPath)
    {
        // 该实现暂不处理额外的依赖项路径.
    }

    /// <summary>
    /// 从指定的完整路径加载程序集.
    /// </summary>
    /// <param name="fullPath">程序集的完整物理路径.</param>
    /// <returns>加载的程序集对象.</returns>
    public Assembly LoadFromPath(string fullPath)
    {
        return this.loadedAssemblies.GetOrAdd(fullPath, path =>
        {
            // 使用默认的 AssemblyLoadContext 确保依赖项能在当前运行上下文中正确解析.
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        });
    }
}