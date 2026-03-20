using BgCommon.Localization.ComponentModel;

namespace BgCommon.Localization;

/// <summary>
/// 多语言对应的枚举信息.
/// </summary>
[TypeConverter(typeof(EnumLocalizationConverter))]
public enum LanguageEnum : int
{
    /// <summary>
    /// 中文简体
    /// </summary>
    [Display(Name = "简体中文", Description = "简体中文备注")]
    Chinese = 2052,

    /// <summary>
    /// 中文台湾
    /// </summary>
    [Display(Name = "繁體中文", Description = "繁體中文备注")]
    ChineseTW = 1028,

    /// <summary>
    /// 英语 - 美国
    /// </summary>
    [Display(Name = "English", Description = "EnglishRemark")]
    EnglishUS = 1033,

    /// <summary>
    /// 越南 - 越南
    /// </summary>
    [Display(Name = "Việt Nam", Description = "Việt Nam_Remark")]
    Vietnam = 1066,
}