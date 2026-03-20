namespace BgCommon;

/// <summary>
/// LINQ扩展方法类.
/// </summary>
public static partial class Extension
{
    /// <summary>
    /// 对序列中的每个元素执行指定的操作.
    /// </summary>
    /// <typeparam name="TSource">源序列元素的类型.</typeparam>
    /// <param name="source">源序列.</param>
    /// <param name="predicate">要对每个元素执行的操作.</param>
    /// <returns>与输入序列相同的序列.</returns>
    public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> predicate)
    {
        var enumerable = source as IList<TSource> ?? source.ToList();
        foreach (var item in enumerable)
        {
            predicate.Invoke(item);
        }

        return enumerable;
    }

    /// <summary>
    /// 对序列中的每个元素执行指定的操作，同时提供元素的索引.
    /// </summary>
    /// <typeparam name="TSource">源序列元素的类型.</typeparam>
    /// <param name="source">源序列.</param>
    /// <param name="predicate">要对每个元素执行的操作，第一个参数是元素，第二个参数是索引.</param>
    /// <returns>与输入序列相同的序列.</returns>
    public static IEnumerable<TSource> DoWithIndex<TSource>(this IEnumerable<TSource> source, Action<TSource, int> predicate)
    {
        var enumerable = source as IList<TSource> ?? source.ToList();
        for (var i = 0; i < enumerable.Count; i++)
        {
            predicate.Invoke(enumerable[i], i);
        }

        return enumerable;
    }
}