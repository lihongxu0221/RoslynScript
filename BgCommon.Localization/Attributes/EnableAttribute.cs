namespace BgCommon.Localization.Attributes;

/// <summary>
/// 指示枚举项是否可用
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class EnableAttribute : Attribute
{
    private readonly bool isEnabled;

    public bool IsEnabled => isEnabled;

    public EnableAttribute(bool isEnable)
    {
        isEnabled = isEnable;
    }
}