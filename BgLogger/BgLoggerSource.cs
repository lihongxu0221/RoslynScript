using BgCommon.Localization.ComponentModel;

namespace BgLogger;

/// <summary>
/// 日志数据源.
/// </summary>
[TypeConverter(typeof(EnumLocalizationConverter))]
public enum BgLoggerSource
{
    /// <summary>
    /// 普通日志
    /// </summary>
    [Display(Name = "运行日志")]
    General = 0,

    /// <summary>
    /// 弹窗日志
    /// </summary>
    [Display(Name = "弹窗日志")]
    Popup,

    /// <summary>
    /// 视觉日志
    /// </summary>
    [Display(Name = "视觉日志")]
    Vision,

    /// <summary>
    /// 运动控制日志
    /// </summary>
    [Display(Name = "运控日志")]
    Motion,

    /// <summary>
    /// 设备日志
    /// </summary>
    [Display(Name = "硬件日志")]
    Hardware,

    /// <summary>
    /// 实时生产和检测
    /// </summary>
    [Display(Name = "生产日志")]
    RealtimeProduction,

    /// <summary>
    /// MES日志
    /// </summary>
    [Display(Name = "MES日志")]
    MES,

    /// <summary>
    /// 数据库日志
    /// </summary>
    [Display(Name = "数据库日志")]
    DataBase = 100,

    /// <summary>
    /// 未定义
    /// </summary>
    [Display(Name = "未定义")]
    UnKnowm = -1,
}