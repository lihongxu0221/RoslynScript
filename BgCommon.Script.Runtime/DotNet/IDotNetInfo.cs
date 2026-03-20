namespace BgCommon.Script.Runtime.DotNet;

/// <summary>
/// 获取有关本地安装的 .NET 运行时和 SDK 信息的接口.
/// </summary>
public interface IDotNetInfo
{
    /// <summary>
    /// 获取当前的 .NET 运行时版本.
    /// </summary>
    /// <returns>返回语义化版本信息对象.</returns>
    SemanticVersion GetCurrentDotNetRuntimeVersion();

    /// <summary>
    /// 定位 .NET 根目录，如果未找到则抛出异常.
    /// </summary>
    /// <returns>返回 .NET 所在的根目录路径.</returns>
    string LocateDotNetRootDirectoryOrThrow();

    /// <summary>
    /// 定位 .NET 根目录.
    /// </summary>
    /// <returns>返回 .NET 所在的根目录路径；如果未找到则返回 null.</returns>
    string? LocateDotNetRootDirectory();

    /// <summary>
    /// 定位 .NET 可执行文件（如 dotnet.exe）的路径，如果未找到则抛出异常.
    /// </summary>
    /// <returns>返回可执行文件的完整路径.</returns>
    string LocateDotNetExecutableOrThrow();

    /// <summary>
    /// 定位 .NET 可执行文件（如 dotnet.exe）的路径.
    /// </summary>
    /// <returns>返回可执行文件的完整路径；如果未找到则返回 null.</returns>
    string? LocateDotNetExecutable();

    /// <summary>
    /// 获取本地安装的所有 .NET 运行时版本列表，如果获取失败则抛出异常.
    /// </summary>
    /// <returns>运行时版本信息数组.</returns>
    DotNetRuntimeVersion[] GetDotNetRuntimeVersionsOrThrow();

    /// <summary>
    /// 获取本地安装的所有 .NET 运行时版本列表.
    /// </summary>
    /// <returns>运行时版本信息数组.</returns>
    DotNetRuntimeVersion[] GetDotNetRuntimeVersions();

    /// <summary>
    /// 获取本地安装的所有 .NET SDK 版本列表，如果获取失败则抛出异常.
    /// </summary>
    /// <returns>SDK 版本信息数组.</returns>
    DotNetSdkVersion[] GetDotNetSdkVersionsOrThrow();

    /// <summary>
    /// 获取本地安装的所有 .NET SDK 版本列表.
    /// </summary>
    /// <returns>SDK 版本信息数组.</returns>
    DotNetSdkVersion[] GetDotNetSdkVersions();

    /// <summary>
    /// 获取最新受支持的 .NET SDK 版本，如果未找到则抛出异常.
    /// </summary>
    /// <param name="includePrerelease">是否包含预览版.</param>
    /// <returns>最新的 SDK 版本信息.</returns>
    DotNetSdkVersion GetLatestSupportedDotNetSdkVersionOrThrow(bool includePrerelease = false);

    /// <summary>
    /// 获取最新受支持的 .NET SDK 版本.
    /// </summary>
    /// <param name="includePrerelease">是否包含预览版.</param>
    /// <returns>最新的 SDK 版本信息；如果未找到则返回 null.</returns>
    DotNetSdkVersion? GetLatestSupportedDotNetSdkVersion(bool includePrerelease = false);

    /// <summary>
    /// 定位 .NET EF (Entity Framework) 工具可执行文件的路径，如果未找到则抛出异常.
    /// </summary>
    /// <returns>EF 工具的完整路径.</returns>
    string LocateDotNetEfToolExecutableOrThrow();

    /// <summary>
    /// 定位 .NET EF (Entity Framework) 工具可执行文件的路径.
    /// </summary>
    /// <returns>EF 工具的完整路径；如果未找到则返回 null.</returns>
    string? LocateDotNetEfToolExecutable();

    /// <summary>
    /// 获取指定路径下 .NET EF 工具的版本信息.
    /// </summary>
    /// <param name="dotNetEfToolExePath">EF 工具可执行文件的完整物理路径.</param>
    /// <returns>语义化版本信息；如果无法解析则返回 null.</returns>
    SemanticVersion? GetDotNetEfToolVersion(string dotNetEfToolExePath);
}