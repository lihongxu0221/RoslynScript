namespace BgCommon.IO;

/// <summary>
/// An absolute path to a file or a directory.
/// </summary>
public abstract record AbsolutePath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsolutePath"/> class.
    /// </summary>
    /// <param name="path">路径字符串.</param>
    public AbsolutePath(string path)
    {
        // 验证参数是否为 null.
        ArgumentNullException.ThrowIfNull(path, nameof(path));

        // 验证路径是否为空字符串或仅包含空格.
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("path cannot be empty or whitespace", nameof(path));
        }

        // 获取系统无效的路径字符集合.
        char[] invalidChars = System.IO.Path.GetInvalidPathChars();

        // 检查路径中是否包含非法字符.
        if (invalidChars.Intersect(path).Any())
        {
            throw new ArgumentException("path contains illegal characters", nameof(path));
        }

        // 去除空格并转换为完整路径后赋值.
        this.Path = System.IO.Path.GetFullPath(path.Trim());
    }

    /// <summary>
    /// Gets 路径的字符串表示形式.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 检查路径指向的文件或目录是否存在.
    /// </summary>
    /// <returns>如果存在则返回 true，否则返回 false.</returns>
    public abstract bool Exists();

    /// <summary>
    /// 如果路径指向的内容存在，则将其删除.
    /// </summary>
    public abstract void DeleteIfExists();

    /// <summary>
    /// 返回表示当前对象的字符串.
    /// </summary>
    /// <returns>路径的字符串表示.</returns>
    public sealed override string ToString()
    {
        // 返回当前实例的路径属性.
        return this.Path;
    }

    /// <summary>
    /// 隐式将 AbsolutePath 转换为 string.
    /// </summary>
    /// <param name="path">AbsolutePath 实例.</param>
    /// <returns>路径字符串.</returns>
    public static implicit operator string(AbsolutePath path) => path.Path;
}