using System.Diagnostics.CodeAnalysis;

namespace BgCommon;

/// <summary>
/// 提供针对各种对象类型的验证与状态检查扩展方法.
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// 检测对象是否为 null，为 null 则抛出 ArgumentNullException 异常.
    /// </summary>
    /// <param name="obj">需要检测的目标对象.</param>
    /// <param name="parameterName">异常信息中显示的参数名称.</param>
    public static void CheckNull(this object? obj, string parameterName)
    {
        // 按照规则 8 使用内置的空检查方法
        ArgumentNullException.ThrowIfNull(obj, parameterName);
    }

    /// <summary>
    /// 判断字符串是否为空、空字符串或仅由空白字符组成.
    /// </summary>
    /// <param name="inputValue">待检查的字符串值.</param>
    /// <returns>如果字符串为空或空白则返回 true，否则返回 false.</returns>
    public static bool IsEmpty([NotNullWhen(false)] this string? inputValue)
    {
        // 使用系统内置方法检查空白字符串
        return string.IsNullOrWhiteSpace(inputValue);
    }

    /// <summary>
    /// 判断 Guid 是否为 Empty（全 0）.
    /// </summary>
    /// <param name="guidValue">待检查的 Guid 值.</param>
    /// <returns>如果是空 Guid 则返回 true，否则返回 false.</returns>
    public static bool IsEmpty(this Guid guidValue)
    {
        return guidValue == Guid.Empty;
    }

    /// <summary>
    /// 判断可空 Guid 是否为 null 或为 Empty.
    /// </summary>
    /// <param name="guidValue">待检查的可空 Guid 值.</param>
    /// <returns>如果为 null 或空 Guid 则返回 true，否则返回 false.</returns>
    public static bool IsEmpty([NotNullWhen(false)] this Guid? guidValue)
    {
        // 检查引用是否为空
        if (guidValue == null)
        {
            return true;
        }

        // 检查 Guid 内容是否为全零
        return guidValue == Guid.Empty;
    }

    /// <summary>
    /// 判断集合是否为 null 或不包含任何元素.
    /// </summary>
    /// <typeparam name="T">集合元素的类型.</typeparam>
    /// <param name="collection">待检查的集合对象.</param>
    /// <returns>如果集合为空或没有任何元素则返回 true，否则返回 false.</returns>
    public static bool IsEmpty<T>(this IEnumerable<T>? collection)
    {
        // 优先检查引用是否为 null
        if (collection == null)
        {
            return true;
        }

        // 使用 Linq 检查是否包含元素
        return !collection.Any();
    }

    /// <summary>
    /// 判断给定的值是否为其类型的默认值.
    /// </summary>
    /// <typeparam name="T">值的类型.</typeparam>
    /// <param name="inputValue">待检查的值.</param>
    /// <returns>如果是默认值则返回 true，否则返回 false.</returns>
    public static bool IsDefault<T>(this T inputValue)
    {
        // 使用 EqualityComparer 处理泛型默认值比较
        return EqualityComparer<T>.Default.Equals(inputValue, default!);
    }
}