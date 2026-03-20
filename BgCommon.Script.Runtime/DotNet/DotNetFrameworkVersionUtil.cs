using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;

namespace BgCommon.Script.Runtime.DotNet;

/// <summary>
/// .NET 框架版本工具类.
/// </summary>
public static class DotNetFrameworkVersionUtil
{
    /// <summary>
    /// 支持的最小 .NET 版本.
    /// </summary>
    public const int MinSupportedDotNetVersion = 6;

    /// <summary>
    /// 支持的最大 .NET 版本.
    /// </summary>
    public const int MaxSupportedDotNetVersion = 10;

    /// <summary>
    /// 支持的最小 EF 工具版本.
    /// </summary>
    public const int MinSupportedEfToolVersion = 5;

    /// <summary>
    /// 主版本号到框架版本的映射字典.
    /// </summary>
    private static readonly Dictionary<int, DotNetFrameworkVersion> MajorToFrameworkVersion = new()
    {
        { 5, DotNetFrameworkVersion.DotNet5 },
        { 6, DotNetFrameworkVersion.DotNet6 },
        { 7, DotNetFrameworkVersion.DotNet7 },
        { 8, DotNetFrameworkVersion.DotNet8 },
        { 9, DotNetFrameworkVersion.DotNet9 },
        { 10, DotNetFrameworkVersion.DotNet10 },
    };

    /// <summary>
    /// 框架版本到主版本号的映射字典.
    /// </summary>
    private static readonly Dictionary<DotNetFrameworkVersion, int> FrameworkVersionToMajor =
        MajorToFrameworkVersion.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>
    /// 框架版本到 TFM (Target Framework Moniker) 的映射字典.
    /// </summary>
    private static readonly Dictionary<DotNetFrameworkVersion, string> FrameworkVersionToTfm = new()
    {
        { DotNetFrameworkVersion.DotNet5, "net5.0" },
        { DotNetFrameworkVersion.DotNet6, "net6.0" },
        { DotNetFrameworkVersion.DotNet7, "net7.0" },
        { DotNetFrameworkVersion.DotNet8, "net8.0" },
        { DotNetFrameworkVersion.DotNet9, "net9.0" },
        { DotNetFrameworkVersion.DotNet10, "net10.0" },
    };

    /// <summary>
    /// TFM 到框架版本的映射字典.
    /// </summary>
    private static readonly Dictionary<string, DotNetFrameworkVersion> TFmToFrameworkVersion =
        FrameworkVersionToTfm.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>
    /// 框架版本到 C# 语言版本的映射字典.
    /// </summary>
    private static readonly Dictionary<DotNetFrameworkVersion, LanguageVersion> FrameworkVersionToLangVersion = new()
    {
        { DotNetFrameworkVersion.DotNet5, LanguageVersion.CSharp9 },
        { DotNetFrameworkVersion.DotNet6, LanguageVersion.CSharp10 },
        { DotNetFrameworkVersion.DotNet7, LanguageVersion.CSharp11 },
        { DotNetFrameworkVersion.DotNet8, LanguageVersion.CSharp12 },
        { DotNetFrameworkVersion.DotNet9, LanguageVersion.CSharp13 },
        { DotNetFrameworkVersion.DotNet10, LanguageVersion.Preview },
    };

    /// <summary>
    /// 检查指定的 SDK 版本是否受支持.
    /// </summary>
    /// <param name="sdkVersion">待检查的 SDK 版本语义化信息.</param>
    /// <returns>如果受支持则返回 true，否则返回 false.</returns>
    public static bool IsSdkVersionSupported(SemanticVersion sdkVersion)
    {
        // 校验版本号是否在定义的最小和最大支持范围内
        return sdkVersion.Major is >= MinSupportedDotNetVersion and <= MaxSupportedDotNetVersion;
    }

    /// <summary>
    /// 检查当前的 SDK 版本是否受支持.
    /// </summary>
    /// <param name="sdkVersion">SDK 版本对象.</param>
    /// <returns>如果受支持则返回 true，否则返回 false.</returns>
    public static bool IsSupported(this DotNetSdkVersion sdkVersion)
    {
        // 调用基础校验逻辑
        return IsSdkVersionSupported(sdkVersion.Version);
    }

    /// <summary>
    /// 检查指定的 EF 工具版本是否受支持.
    /// </summary>
    /// <param name="efToolVersion">待检查的 EF 工具版本语义化信息.</param>
    /// <returns>如果受支持则返回 true，否则返回 false.</returns>
    public static bool IsEfToolVersionSupported(SemanticVersion efToolVersion)
    {
        // 校验 EF 工具主版本是否大于等于最小支持版本
        return efToolVersion.Major >= MinSupportedEfToolVersion;
    }

    /// <summary>
    /// 获取框架版本对应的目标框架名字 (TFM).
    /// </summary>
    /// <param name="frameworkVersion">框架版本枚举.</param>
    /// <returns>目标框架名字字符串.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当框架版本未知时抛出异常.</exception>
    public static string GetTargetFrameworkMoniker(this DotNetFrameworkVersion frameworkVersion)
    {
        // 尝试从映射字典中获取对应的 TFM 字符串
        return FrameworkVersionToTfm.TryGetValue(frameworkVersion, out var targetFrameworkMoniker)
            ? targetFrameworkMoniker
            : throw new ArgumentOutOfRangeException(nameof(frameworkVersion), frameworkVersion, $"Unknown framework version: {frameworkVersion}");
    }

    /// <summary>
    /// 根据 TFM 获取对应的框架版本枚举.
    /// </summary>
    /// <param name="targetFrameworkMoniker">目标框架名字字符串.</param>
    /// <returns>框架版本枚举.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 TFM 未知时抛出异常.</exception>
    public static DotNetFrameworkVersion GetFrameworkVersion(string targetFrameworkMoniker)
    {
        // 尝试获取框架版本，若失败则抛出异常
        return TryGetFrameworkVersion(targetFrameworkMoniker, out var resultVersion)
            ? resultVersion.Value
            : throw new ArgumentOutOfRangeException(
                nameof(targetFrameworkMoniker),
                targetFrameworkMoniker,
                $"Unknown target framework moniker (TFM): {targetFrameworkMoniker}");
    }

    /// <summary>
    /// 尝试根据 TFM 获取对应的框架版本枚举.
    /// </summary>
    /// <param name="targetFrameworkMoniker">目标框架名字字符串.</param>
    /// <param name="frameworkVersion">输出的框架版本枚举，若未找到则为 null.</param>
    /// <returns>如果获取成功则返回 true，否则返回 false.</returns>
    public static bool TryGetFrameworkVersion(string targetFrameworkMoniker, [NotNullWhen(true)] out DotNetFrameworkVersion? frameworkVersion)
    {
        // 在 TFM 映射表中查找
        if (TFmToFrameworkVersion.TryGetValue(targetFrameworkMoniker, out var matchedVersion))
        {
            frameworkVersion = matchedVersion;
            return true;
        }

        frameworkVersion = null;
        return false;
    }

    /// <summary>
    /// 根据运行时版本获取框架版本.
    /// </summary>
    /// <param name="runtimeVersion">运行时版本对象.</param>
    /// <returns>框架版本枚举.</returns>
    public static DotNetFrameworkVersion GetFrameworkVersion(this DotNetRuntimeVersion runtimeVersion)
    {
        // 提取主版本号并转换
        return GetFrameworkVersion(runtimeVersion.Version.Major);
    }

    /// <summary>
    /// 根据 SDK 版本获取框架版本.
    /// </summary>
    /// <param name="sdkVersion">SDK 版本对象.</param>
    /// <returns>框架版本枚举.</returns>
    public static DotNetFrameworkVersion GetFrameworkVersion(this DotNetSdkVersion sdkVersion)
    {
        // 提取主版本号并转换
        return GetFrameworkVersion(sdkVersion.Version.Major);
    }

    /// <summary>
    /// 根据主版本号获取框架版本.
    /// </summary>
    /// <param name="majorVersion">主版本号.</param>
    /// <returns>框架版本枚举.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当主版本号未知时抛出异常.</exception>
    public static DotNetFrameworkVersion GetFrameworkVersion(int majorVersion)
    {
        // 从主版本号映射字典中查询
        if (MajorToFrameworkVersion.TryGetValue(majorVersion, out var matchedFrameworkVersion))
        {
            return matchedFrameworkVersion;
        }

        throw new ArgumentOutOfRangeException(nameof(majorVersion), majorVersion, $"Unknown major version: {majorVersion}");
    }

    /// <summary>
    /// 获取框架版本对应的主版本号.
    /// </summary>
    /// <param name="frameworkVersion">框架版本枚举.</param>
    /// <returns>主版本号.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当框架版本未知时抛出异常.</exception>
    public static int GetMajorVersion(this DotNetFrameworkVersion frameworkVersion)
    {
        // 从框架版本映射字典中查询主版本号
        if (!FrameworkVersionToMajor.TryGetValue(frameworkVersion, out int resultMajorVersion))
        {
            throw new ArgumentOutOfRangeException(nameof(frameworkVersion), frameworkVersion, $"Unknown framework version: {frameworkVersion}");
        }
        return resultMajorVersion;
    }

    /// <summary>
    /// 获取框架版本支持的最新 C# 语言版本.
    /// </summary>
    /// <param name="dotNetFrameworkVersion">框架版本枚举.</param>
    /// <returns>C# 语言版本枚举.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当框架版本未知时抛出异常.</exception>
    public static LanguageVersion GetLatestSupportedCSharpLanguageVersion(this DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        // 从语言版本映射字典中查询
        return FrameworkVersionToLangVersion.TryGetValue(dotNetFrameworkVersion, out var matchedLanguageVersion)
            ? matchedLanguageVersion
            : throw new ArgumentOutOfRangeException(
                nameof(dotNetFrameworkVersion),
                dotNetFrameworkVersion,
                $"Unknown framework version: {dotNetFrameworkVersion}");
    }
}