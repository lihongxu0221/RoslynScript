namespace BgCommon.Localization.Converters;

/// <summary>
/// 多语言转换器.<br/>
/// 不支持实时更新.<br/>
/// ConvertParameter 为程序集名称.<br/>
/// </summary>
public class LocalizationConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string key && !string.IsNullOrEmpty(key))
        {
            string? assemblyName = System.Convert.ToString(parameter);
            return LocalizationProviderFactory.GetString(assemblyName, key) ?? value;
        }

        return value;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}