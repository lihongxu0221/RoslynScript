namespace BgCommon.Script.Runtime.Compilation;

/// <summary>
/// 表示当脚本引用无效时抛出的异常.
/// </summary>
public class InvalidReferenceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidReferenceException"/> class.
    /// </summary>
    /// <param name="reference">导致异常的引用实例.</param>
    /// <param name="message">描述错误的消息.</param>
    public InvalidReferenceException(LibraryRef reference, string message)
        : base(message)
    {
        // 验证引用参数不为空
        ArgumentNullException.ThrowIfNull(reference, nameof(reference));

        // 赋值引用实例
        this.Reference = reference;
    }

    /// <summary>
    /// Gets 导致异常的引用实例.
    /// </summary>
    public LibraryRef Reference { get; }
}