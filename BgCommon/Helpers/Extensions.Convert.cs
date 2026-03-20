using System;
using System.Collections.Generic;
using System.Linq;

namespace BgCommon;

/// <summary>
/// 提供类型转换与数据处理的静态扩展方法类.
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// 将对象安全地转换为字符串并去除两端空格.
    /// </summary>
    /// <param name="input">需要转换的输入对象.</param>
    /// <returns>转换后的字符串。如果输入为 null，则返回空字符串.</returns>
    public static string SafeString(this object? input)
    {
        // 尝试调用 ToString 并去除空格，若结果为空则返回空字符串.
        return input?.ToString()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// 将字符串转换为布尔值.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的布尔结果.</returns>
    public static bool ToBool(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToBool(obj);
    }

    /// <summary>
    /// 将字符串转换为可空布尔值.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的可空布尔结果.</returns>
    public static bool? ToBoolOrNull(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToBoolOrNull(obj);
    }

    /// <summary>
    /// 将字符串转换为 32 位整型.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的整数结果.</returns>
    public static int ToInt(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToInt(obj);
    }

    /// <summary>
    /// 将字符串转换为 32 位可空整型.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的可空整数结果.</returns>
    public static int? ToIntOrNull(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToIntOrNull(obj);
    }

    /// <summary>
    /// 将字符串转换为 64 位整型.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的长整数结果.</returns>
    public static long ToLong(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToLong(obj);
    }

    /// <summary>
    /// 将字符串转换为 64 位可空整型.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的可空长整数结果.</returns>
    public static long? ToLongOrNull(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToLongOrNull(obj);
    }

    /// <summary>
    /// 将字符串转换为双精度浮点型.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的双精度浮点结果.</returns>
    public static double ToDouble(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToDouble(obj);
    }

    /// <summary>
    /// 将字符串转换为可空双精度浮点型.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的可空双精度浮点结果.</returns>
    public static double? ToDoubleOrNull(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToDoubleOrNull(obj);
    }

    /// <summary>
    /// 将字符串转换为高精度浮点型.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的高精度浮点结果.</returns>
    public static decimal ToDecimal(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToDecimal(obj);
    }

    /// <summary>
    /// 将字符串转换为可空高精度浮点型.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的可空高精度浮点结果.</returns>
    public static decimal? ToDecimalOrNull(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToDecimalOrNull(obj);
    }

    /// <summary>
    /// 将字符串转换为日期时间.
    /// </summary>
    /// <param name="obj">待转换的字符串数据说明.</param>
    /// <returns>转换后的日期时间结果.</returns>
    public static DateTime ToDateTime(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToDateTime(obj);
    }

    /// <summary>
    /// 将字符串转换为可空日期时间.
    /// </summary>
    /// <param name="obj">待转换的字符串数据说明.</param>
    /// <returns>转换后的可空日期时间结果.</returns>
    public static DateTime? ToDateTimeOrNull(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToDateTimeOrNull(obj);
    }

    /// <summary>
    /// 将字符串转换为 Guid.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的 Guid 结果.</returns>
    public static Guid ToGuid(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToGuid(obj);
    }

    /// <summary>
    /// 将字符串转换为可空 Guid.
    /// </summary>
    /// <param name="obj">待转换的字符串数据.</param>
    /// <returns>转换后的可空 Guid 结果.</returns>
    public static Guid? ToGuidOrNull(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToGuidOrNull(obj);
    }

    /// <summary>
    /// 将包含多个 Guid 字符串（逗号分隔）的字符串转换为 Guid 集合.
    /// </summary>
    /// <param name="obj">包含 Guid 字符串的数据，范例: "83B0233C-A24F...".</param>
    /// <returns>解析后的 Guid 列表.</returns>
    public static List<Guid> ToGuidList(this string obj)
    {
        // 调用转换工具类执行逻辑.
        return BgConvert.ToGuidList(obj);
    }

    /// <summary>
    /// 将字符串集合转换为 Guid 集合.
    /// </summary>
    /// <param name="obj">待转换的字符串集合.</param>
    /// <returns>转换后的 Guid 列表.</returns>
    public static List<Guid> ToGuidList(this IList<string> obj)
    {
        // 检查输入集合是否为 null.
        if (obj == null)
        {
            return new List<Guid>();
        }

        // 遍历集合并逐个调用字符串转 Guid 的扩展方法.
        return obj.Select(item => item.ToGuid()).ToList();
    }
}