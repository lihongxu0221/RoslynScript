namespace BgCommon.Script.Runtime.DotNet;

/// <summary>
/// 表示 .NET 运行时的版本信息类.
/// </summary>
public class DotNetRuntimeVersion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotNetRuntimeVersion"/> class.
    /// </summary>
    /// <param name="frameworkName">运行时框架的名称，例如 "Microsoft.NETCore.App" 或 "Microsoft.AspNetCore.App".</param>
    /// <param name="version">语义化版本对象.</param>
    public DotNetRuntimeVersion(string frameworkName, SemanticVersion version)
    {
        ArgumentNullException.ThrowIfNull(frameworkName, nameof(frameworkName));
        ArgumentNullException.ThrowIfNull(version, nameof(version));
        this.FrameworkName = frameworkName;
        this.Version = version;
    }

    /// <summary>
    /// Gets 运行时框架的名称.
    /// </summary>
    public string FrameworkName { get; }

    /// <summary>
    /// Gets 具体的版本信息.
    /// </summary>
    public SemanticVersion Version { get; }

    /// <summary>
    /// 返回表示当前对象的字符串，包含框架名称和版本号.
    /// </summary>
    /// <returns>组合后的描述字符串.</returns>
    public override string ToString()
    {
        // 拼接框架名称与版本号字符串
        return $"{this.FrameworkName} {this.Version}";
    }
}