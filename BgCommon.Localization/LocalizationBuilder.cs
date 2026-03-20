using BgCommon.Localization.IO;
using System.Diagnostics;

namespace BgCommon.Localization;

/// <summary>
/// 提供为不同区域性构建本地化字符串集合的功能.
/// </summary>
public class LocalizationBuilder
{
    private readonly HashSet<LocalizationSet> localizations = new HashSet<LocalizationSet>();

    private CultureInfo? selectedCulture;

    /// <summary>
    /// 使用当前区域性和本地化构建<see cref="ILocalizationProvider"/>.
    /// </summary>
    /// <returns>An <see cref="ILocalizationProvider"/> 与当前的文化和本地化.</returns>
    public virtual ILocalizationProvider Build()
    {
        return new LocalizationProvider(selectedCulture ?? CultureInfo.CurrentCulture, localizations);
    }

    /// <summary>
    /// 设置<see cref="LocalizationBuilder"/>的区域性.
    /// </summary>
    /// <param name="culture">要设置的区域性.</param>
    public virtual void SetCulture(CultureInfo culture)
    {
        selectedCulture = culture;
    }

    /// <summary>
    /// 将本地化集添加到集合中.
    /// </summary>
    /// <param name="localization">要添加的本地化集.</param>
    /// <exception cref="InvalidOperationException">当集合中已存在相同区域性的本地化集时抛出.</exception>
    public virtual void AddLocalization(LocalizationSet localization)
    {
        if (!localizations.Any(x => x.Name == localization.Name && x.Culture.Equals(localization.Culture)))
        {
            _ = localizations.Add(localization);
            return;
        }

        throw new LocalizationBuilderException($"Localization \"{localization.Name}\" for culture {localization.Culture} already exists.");
    }

    /// <summary>
    /// 将调用程序集中资源的本地化字符串添加到<see cref="LocalizationBuilder"/>.
    /// </summary>
    /// <typeparam name="TResource">资源的类型.</typeparam>
    /// <param name="isPublic">是否为公用的多语言资源.</param>
    public virtual void FromResource<TResource>(bool isPublic)
    {
        Type resourceType = typeof(TResource);
        string? resourceName = resourceType.FullName;

        if (resourceName is null)
        {
            return;
        }

        int[] langs = Enum.GetValues(typeof(LanguageEnum)).Cast<int>().ToArray();
        for (int i = 0; i < langs.Length; i++)
        {
            var cultureInfo = new CultureInfo(langs[i]);
            FromResource(resourceType.Assembly, resourceName, cultureInfo, isPublic);
        }
    }

    /// <summary>
    /// 将调用程序集中资源的本地化字符串添加到<see cref="LocalizationBuilder"/>.
    /// </summary>
    /// <typeparam name="TResource">资源的类型.</typeparam>
    /// <param name="culture">提供本地化字符串的区域语言文化.</param>
    /// <param name="isPublic">是否为公用的多语言资源.</param>
    public virtual void FromResource<TResource>(CultureInfo culture, bool isPublic)
    {
        Type resourceType = typeof(TResource);
        string? resourceName = resourceType.FullName;

        if (resourceName is null)
        {
            return;
        }

        FromResource(resourceType.Assembly, resourceName, culture, isPublic);
    }

    /// <summary>
    /// 将指定程序集中具有指定基名称的资源中的本地化字符串添加到<see cref="LocalizationBuilder"/>.
    /// </summary>
    /// <param name="assembly">包含资源的程序集.</param>
    /// <param name="baseName">资源的基本名称.</param>
    /// <param name="culture">提供本地化字符串的区域性.</param>
    /// <param name="isPublic">是否为公用的多语言资源.</param>
    public virtual void FromResource(Assembly assembly, string baseName, CultureInfo culture, bool isPublic)
    {
        LocalizationSet? localizationSet = LocalizationSetResourceParser.Parse(
            assembly,
            baseName,
            culture,
            isPublic);

        if (localizationSet is not null)
        {
            AddLocalization(localizationSet);
        }
    }
}