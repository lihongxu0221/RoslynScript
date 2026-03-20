using BgCommon.Script.Runtime.CodeAnalysis;
using BgCommon.Script.Runtime.Compilation;
using BgCommon.Script.Runtime.Compilation.CSharp;
using BgCommon.Script.Runtime.Configuration;
using BgCommon.Script.Runtime.DotNet;
using BgCommon.Script.Runtime.Services;
using BgCommon.Script.Runtime.Themes;
using Newtonsoft.Json.Linq;

namespace BgCommon.Script.Runtime;

/// <summary>
/// 脚本工厂类，负责初始化环境、管理主题及执行平台.
/// </summary>
public class ScriptFactory
{
    public static readonly IReadOnlyList<LibraryRef> DefaultReferences = new List<LibraryRef>
    {
        LibraryRef.FrameworkReference(typeof(object).Assembly.GetName().FullName),
        LibraryRef.FrameworkReference(typeof(Console).Assembly.GetName().FullName),
        LibraryRef.FrameworkReference(typeof(Enumerable).Assembly.GetName().FullName),
        LibraryRef.FrameworkReference(typeof(System.ComponentModel.Component).Assembly.GetName().FullName),
        LibraryRef.FrameworkReference(typeof(System.Text.Json.JsonSerializer).Assembly.GetName().FullName),
        LibraryRef.FrameworkReference("System.Runtime"),
        LibraryRef.FrameworkReference("netstandard"),
        LibraryRef.FrameworkReference("WindowsBase"),
        LibraryRef.Reference(Path.Combine(AppContext.BaseDirectory, "HandyControl.dll")),
        LibraryRef.Reference(Path.Combine(AppContext.BaseDirectory, "CommunityToolkit.Mvvm.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BgBase.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BgMotion.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VM.Core.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BgCommon.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BgLogger.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BgCommon.Localization.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BgControls.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BgCommon.Prism.Wpf.dll")),
        LibraryRef.Reference(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RoslynPad.Runtime.dll")),
    };

    public static readonly IReadOnlyList<string> DefaultUsings = new List<string>
    {
        "System",
        "System.Collections.Generic",
        "System.Diagnostics", // 解决 Debug 找不到的问题
        "System.IO",
        "System.Linq",
        "System.Text",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Windows.Threading",
        "BgLogger",
        "BgCommon",
        "BgCommon.Localization",
        "BgCommon.Prism.Wpf",
        "BgCommon.Prism.Wpf.Controls",
        "BgCommon.Prism.Wpf.Controls.Models",
        "BgCommon.Script", // 允许脚本直接识别 ScriptGlobals 类型
        "BgCommon.Script.Runtime",
        "CommunityToolkit.Mvvm.ComponentModel",
        "CommunityToolkit.Mvvm.Input",
        "RoslynPad.Runtime",
    };

    private static readonly Lazy<ScriptFactory> instance =
        new Lazy<ScriptFactory>(() => new ScriptFactory(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static ScriptFactory Instance => instance.Value;

    private IContainerExtension container;
    private bool isInitialized;
    private readonly object initializerLock = new object();
    private ThemeType themeType = ThemeType.Dark;

    private ScriptFactory()
    {
    }

    /// <summary>
    /// Gets a value indicating whether 是否为深色主题.
    /// </summary>
    public bool IsDarkTheme => ThemeType == ThemeType.Dark;

    /// <summary>
    /// Gets 当前的主题类型.
    /// </summary>
    public ThemeType ThemeType => themeType;

    /// <summary>
    /// 工厂Ioc容器初始化.
    /// </summary>
    /// <param name="container">prism ioc 容器实例.</param>
    public void Initialize(IContainerExtension container)
    {
        ArgumentNullException.ThrowIfNull(container, nameof(container));

        // 增加双重检查锁定，确保初始化逻辑只执行一次且线程安全
        if (isInitialized)
        {
            return;
        }

        lock (initializerLock)
        {
            if (isInitialized)
            {
                return;
            }

            _ = this.WarmUpAsync();

            container = container ?? throw new ArgumentNullException(nameof(container));
            _ = container.RegisterSingleton<ITelemetryProvider, TelemetryProvider>();
            _ = container.RegisterSingleton<IApplicationSettings, ApplicationSettings>();
            _ = container.RegisterSingleton<IDotNetInfo, DotNetInfo>();
            _ = container.RegisterSingleton<ICodeAnalysisService, CodeAnalysisService>();
            _ = container.Register<ICodeCompiler, CSharpCodeCompiler>();

            isInitialized = true;
        }
    }

    /// <summary>
    /// 创建一个编译器实例.
    /// </summary>
    /// <returns>返回编译器实例.</returns>
    public ICodeCompiler CreateCompiler()
    {
        return this.container.Resolve<ICodeCompiler>();
    }

    /// <summary>
    /// 由于首次执行时 C# 脚本引擎需要加载和编译相关程序集，可能会有较长的延迟。调用此方法可以提前加载引擎，减少后续执行的响应时间.
    /// </summary>
    /// <returns>预热任务.</returns>
    private async Task WarmUpAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // 1. 定义一个最简单的代码片段
                string code = "public class W { public static int Run() => 1; }";

                // 2. 预热语法树解析
                var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);

                // 3. 预热编译核心 (这是最耗时的部分)
                // 这里使用一个最基础的引用，确保元数据解析器被启动
                var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                    "WarmUpAssembly",
                    new[] { syntaxTree },
                    new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                    new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

                // 4. 预热 Emit 过程 (IL 生成和元数据写入)
                using (var ms = new MemoryStream())
                {
                    var result = compilation.Emit(ms);
                }
            }
            catch
            {
                // 暖场失败不应影响主程序运行
            }
        });
    }
}