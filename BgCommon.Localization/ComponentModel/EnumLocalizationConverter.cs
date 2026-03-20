namespace BgCommon.Localization.ComponentModel;

/// <summary>
/// 枚举多语言转换器.
/// </summary>
public class EnumLocalizationConverter : EnumConverter
{
    public EnumLocalizationConverter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        : base(type)
    {
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is Enum enumValue && enumValue != null)
        {
            return enumValue.GetEnumModel()?.Display ?? value;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return base.ConvertFrom(context, culture, value);
    }
}