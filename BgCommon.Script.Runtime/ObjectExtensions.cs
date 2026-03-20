using System.Runtime.CompilerServices;

namespace BgCommon.Script.Runtime;

/// <summary>
/// 提供对象的扩展工具方法类.
/// </summary>
internal static class ObjectExtensions
{
    /// <summary>
    /// 确保对象不为 null，如果为 null 则抛出 <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <typeparam name="T">对象所属的类型.</typeparam>
    /// <param name="value">需要检查的对象实例.</param>
    /// <param name="expression">由编译器生成的调用方表达式字符串.</param>
    /// <returns>返回非空的对象实例.</returns>
    public static T NotNull<T>(this T? value, [CallerArgumentExpression(nameof(value))] string expression = "")
    {
        // 检查输入值是否为空
        if (value == null)
        {
            // 如果为空，抛出包含表达式信息的异常
            throw new InvalidOperationException("Expression not expected to be null: " + expression);
        }

        // 返回非空结果
        return value;
    }
}