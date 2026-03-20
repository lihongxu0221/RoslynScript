using BgCommon.Script.Runtime.DotNet;
using System.Collections.Concurrent;

namespace BgCommon.Script.Runtime.Compilation;

/// <summary>
/// 用于获取 .NET 框架自身程序集的工具类.
/// </summary>
public static class FrameworkAssemblies
{
    /// <summary>
    /// 系统程序集位置的缓存字典.
    /// </summary>
    private static readonly ConcurrentDictionary<CacheKey, HashSet<string>> SystemAssembliesLocations = new();

    /// <summary>
    /// 获取指定 .NET 框架版本的程序集位置.
    /// </summary>
    /// <param name="dotNetRootDir">.NET 在本地机器上的安装根目录.</param>
    /// <param name="dotNetFrameworkVersion">目标的 .NET 框架版本.</param>
    /// <param name="includeAspNet">是否包含 ASP.NET 程序集.</param>
    /// <returns>程序集文件路径的哈希集合.</returns>
    public static HashSet<string> GetAssemblyLocations(
        DirectoryPath dotNetRootDir,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        bool includeAspNet)
    {
        // 创建缓存键
        CacheKey key = new CacheKey(dotNetFrameworkVersion, includeAspNet);

        // 从缓存中获取或生成程序集路径集合
        return SystemAssembliesLocations.GetOrAdd(
                key,
                static (currentKey, rootPath) =>
                    GetReferenceAssemblyLocationsFromDotNetRoot(rootPath, currentKey.DotNetFrameworkVersion, currentKey.IncludeAspNet),
                dotNetRootDir).ToHashSet();
    }

    /// <summary>
    /// 从当前应用程序域获取实现程序集的位置.
    /// </summary>
    /// <returns>程序集路径集合.</returns>
    private static HashSet<string> GetImplementationAssemblyLocationsFromAppDomain()
    {
        // 获取当前域中所有非动态且具有路径的系统程序集
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
                !assembly.IsDynamic &&
                !string.IsNullOrWhiteSpace(assembly.Location) &&
                assembly.GetName().Name?.StartsWith("System.") == true)
            .Select(assembly => assembly.Location)
            .ToHashSet();
    }

    /// <summary>
    /// 从应用程序上下文中获取受信任的平台程序集位置.
    /// </summary>
    /// <returns>程序集路径集合.</returns>
    /// <exception cref="ArgumentNullException">当 TRUSTED_PLATFORM_ASSEMBLIES 数据为空时抛出.</exception>
    private static HashSet<string> GetImplementationAssemblyLocationsFromAppContext()
    {
        // 从 AppContext 获取受信任的程序集路径字符串
        string? assemblyPaths = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        // 如果路径为空，则抛出异常
        if (string.IsNullOrWhiteSpace(assemblyPaths))
        {
            throw new ArgumentNullException(nameof(assemblyPaths), "TRUSTED_PLATFORM_ASSEMBLIES is empty. Make sure you are not running the app as a Single File application.");
        }

        // 定义需要包含的前缀列表
        string[] includeList = { "System.", "mscorlib.", "netstandard." };

        // 分割路径并过滤出符合条件的程序集
        return assemblyPaths
            .Split(Path.PathSeparator)
            .Where(path =>
            {
                string fileName = Path.GetFileName(path);
                return includeList.Any(prefix => fileName.StartsWith(prefix));
            })
            .ToHashSet();
    }

    /// <summary>
    /// 从 .NET 根目录获取参考程序集的位置.
    /// </summary>
    /// <param name="dotNetRootDir">.NET 安装根目录.</param>
    /// <param name="dotNetFrameworkVersion">.NET 框架版本.</param>
    /// <param name="includeAspNet">是否包含 ASP.NET.</param>
    /// <returns>参考程序集路径集合.</returns>
    /// <exception cref="Exception">当无法定位参考或实现程序集时抛出.</exception>
    private static HashSet<string> GetReferenceAssemblyLocationsFromDotNetRoot(
        DirectoryPath dotNetRootDir,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        bool includeAspNet)
    {
        // 获取参考程序集目录
        List<string>? assemblyDirectories =
            GetReferenceAssemblyDirectories(dotNetRootDir.Path, dotNetFrameworkVersion, includeAspNet);

        // 如果找不到参考程序集，尝试获取实现程序集目录
        if (assemblyDirectories == null)
        {
            string? implementationAssemblyDir =
                GetImplementationAssemblyDirectory(dotNetRootDir.Path, dotNetFrameworkVersion);
            if (implementationAssemblyDir != null)
            {
                assemblyDirectories = [implementationAssemblyDir];
            }
        }

        // 如果最终未找到任何目录，抛出异常
        if (assemblyDirectories == null || assemblyDirectories.Count == 0)
        {
            throw new DirectoryNotFoundException($"Could not locate .NET {dotNetFrameworkVersion.GetMajorVersion()} SDK reference or implementation assemblies using .NET SDK root: {dotNetRootDir.Path}");
        }

        // 遍历目录获取所有 dll 文件，排除 VisualBasic 相关程序集
        return assemblyDirectories
            .SelectMany(dir => Directory.GetFiles(dir, "*.dll"))
            .Where(filePath => !filePath.Contains("VisualBasic"))
            .ToHashSet();
    }

    /// <summary>
    /// 获取特定 .NET 版本的参考程序集目录路径.
    /// </summary>
    /// <param name="dotnetRoot">.NET SDK 安装的绝对路径.</param>
    /// <param name="dotNetFrameworkVersion">.NET 版本.</param>
    /// <param name="includeAspNet">是否包含 ASP.NET Core 参考程序集目录.</param>
    /// <returns>目录路径列表，如果未找到关键目录则返回 null.</returns>
    private static List<string>? GetReferenceAssemblyDirectories(
        string dotnetRoot,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        bool includeAspNet)
    {
        List<string> directories = new List<string>();
        int majorVersion = dotNetFrameworkVersion.GetMajorVersion();

        // 添加核心运行时的参考目录
        if (!AddDir("Microsoft.NETCore.App.Ref"))
        {
            return null;
        }

        // 如果需要，添加 ASP.NET 的参考目录
        if (includeAspNet && !AddDir("Microsoft.AspNetCore.App.Ref"))
        {
            return null;
        }

        return directories;

        // 本地函数：用于添加特定包的参考路径
        bool AddDir(string packName)
        {
            DirectoryInfo referenceAssemblyRoot = new DirectoryInfo(Path.Combine(dotnetRoot, "packs", packName));

            // 获取最新的次要版本目录
            string? latestMinorVersionDir = GetLatestVersionDir(referenceAssemblyRoot, majorVersion)?.Name;

            if (latestMinorVersionDir != null)
            {
                string target = Path.Combine(
                    referenceAssemblyRoot.FullName,
                    latestMinorVersionDir,
                    "ref",
                    $"net{majorVersion}.0");

                if (!Directory.Exists(target))
                {
                    return false;
                }

                directories.Add(target);

                // 检查并添加分析器路径
                string analysers = Path.Combine(
                    referenceAssemblyRoot.FullName,
                    latestMinorVersionDir,
                    "analyzers",
                    "dotnet",
                    "cs");

                if (Directory.Exists(analysers))
                {
                    directories.Add(analysers);
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 获取特定 .NET 版本的实现程序集目录路径.
    /// </summary>
    /// <param name="dotnetRoot">.NET SDK 安装的绝对路径.</param>
    /// <param name="dotNetFrameworkVersion">.NET 版本.</param>
    /// <returns>实现程序集目录路径，若不存在则返回 null.</returns>
    private static string? GetImplementationAssemblyDirectory(
        string dotnetRoot,
        DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        // 实现程序集的根目录
        DirectoryInfo runtimeImplementationAssemblyRoot =
            new DirectoryInfo(Path.Combine(dotnetRoot, "shared", "Microsoft.NETCore.App"));

        // 获取适用的最新版本目录名
        string? latestApplicableDirName =
            GetLatestVersionDir(runtimeImplementationAssemblyRoot, dotNetFrameworkVersion.GetMajorVersion())?.Name;

        if (latestApplicableDirName == null)
        {
            return null;
        }

        // 拼接完整的实现程序集路径
        string implementationAssembliesDir =
            Path.Combine(runtimeImplementationAssemblyRoot.FullName, latestApplicableDirName);

        return !Directory.Exists(implementationAssembliesDir) ? null : implementationAssembliesDir;
    }

    /// <summary>
    /// 在指定根目录下获取匹配目标主版本号的最新版本目录.
    /// </summary>
    /// <param name="root">根目录信息.</param>
    /// <param name="targetMajorVersion">目标主版本号.</param>
    /// <returns>最新的目录信息，若未找到则返回 null.</returns>
    private static DirectoryInfo? GetLatestVersionDir(DirectoryInfo root, int targetMajorVersion)
    {
        if (!root.Exists)
        {
            return null;
        }

        // 查找所有子目录，尝试解析为语义化版本，并过滤出主版本匹配的最高版本
        return root.GetDirectories()
            .Select(dir => SemanticVersion.TryParse(dir.Name, out var version)
                ? new { Directory = dir, Version = version }
                : null)
            .Where(item => item != null && item.Version.Major == targetMajorVersion)
            .MaxBy(item => item!.Version)?
            .Directory;
    }

    /// <summary>
    /// 用于缓存程序集查找结果的键.
    /// </summary>
    /// <param name="DotNetFrameworkVersion">.NET 框架版本.</param>
    /// <param name="IncludeAspNet">Gets a value indicating whether 是否包含 ASP.NET.</param>
    private record CacheKey(DotNetFrameworkVersion DotNetFrameworkVersion, bool IncludeAspNet);
}