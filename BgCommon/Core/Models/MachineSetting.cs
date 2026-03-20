namespace BgCommon.Core.Models;

/// <summary>
/// 机器设置模型.
/// </summary>
public partial class MachineSetting : ObservableObject, ICloneable, IEquatable<MachineSetting>
{
    /// <summary>
    /// Gets or sets 机器名称.
    /// </summary>
    [ObservableProperty]
    private string machineName = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether 是否为本地设备.
    /// </summary>
    [ObservableProperty]
    private bool isLocal = false;

    /// <summary>
    /// Gets or sets a value indicating whether 当前使用中的设备.
    /// </summary>
    [ObservableProperty]
    private bool isUsed = false;

    /// <summary>
    /// Gets or sets 机器名称.
    /// </summary>
    [ObservableProperty]
    private string ipAddress = string.Empty;

    /// <summary>
    /// Gets or sets 配置文件存储路径.
    /// </summary>
    [ObservableProperty]
    private string configDirectory = string.Empty;

    /// <summary>
    /// Gets or sets 项目存储路径.
    /// </summary>
    [ObservableProperty]
    private string projectDirectory = string.Empty;

    /// <summary>
    /// Gets or sets 项目结果存储路径.
    /// </summary>
    [ObservableProperty]
    private string projectResultDirectory = string.Empty;

    /// <inheritdoc/>
    public object Clone()
    {
        return this.MemberwiseClone();
    }

    /// <inheritdoc/>
    public bool Equals(MachineSetting? other)
    {
        if (ReferenceEquals(default(MachineSetting), other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this.MachineName == other.MachineName &&
               this.IpAddress == other.IpAddress;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as MachineSetting);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            this.MachineName,
            this.IpAddress);
    }
}
