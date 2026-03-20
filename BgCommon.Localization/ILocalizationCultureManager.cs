namespace BgCommon.Localization;

/// <summary>
/// Defines methods for managing localization settings.
/// </summary>
public interface ILocalizationCultureManager
{
    /// <summary>
    /// Sets the current culture for localization.
    /// </summary>
    /// <param name="culture">The culture to set.</param>
    void SetCulture(CultureInfo culture);

    /// <summary>
    /// Gets the current culture used for localization.
    /// </summary>
    /// <returns>The current culture.</returns>
    CultureInfo GetCulture();
}