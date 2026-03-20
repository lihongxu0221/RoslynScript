namespace BgCommon.IO;

/// <summary>
/// An absolute path to a directory.
/// </summary>
public record DirectoryPath : AbsolutePath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryPath"/> class.
    /// </summary>
    /// <param name="path">目录路径字符串.</param>
    public DirectoryPath(string path)
        : base(path)
    {
    }

    /// <summary>
    /// 确定指定的对象是否等于当前对象.
    /// </summary>
    /// <param name="other">要与当前对象进行比较的对象.</param>
    /// <returns>如果指定的对象等于当前对象，则为 true；否则为 false.</returns>
    public virtual bool Equals(DirectoryPath? other)
    {
        // 比较路径字符串，根据操作系统决定是否忽略大小写
        return this.Path.Equals(
            other?.Path,
            PlatformUtil.IsOSWindows() ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
    }

    /// <summary>
    /// 作为默认哈希函数.
    /// </summary>
    /// <returns>当前对象的哈希代码.</returns>
    public override int GetHashCode()
    {
        return this.Path.ToLowerInvariant().GetHashCode();
    }

    /// <summary>
    /// 获取一个值，该值指示当前路径是否存在.
    /// </summary>
    /// <returns>如果存在则返回 true.</returns>
    public override bool Exists()
    {
        return Directory.Exists(this.Path);
    }

    /// <summary>
    /// 如果目录存在则删除.
    /// </summary>
    public override void DeleteIfExists()
    {
        if (this.Exists())
        {
            // 递归删除目录
            Directory.Delete(this.Path, true);
        }
    }

    /// <summary>
    /// 获取目录信息对象.
    /// </summary>
    /// <returns>DirectoryInfo 对象.</returns>
    public DirectoryInfo GetInfo()
    {
        return new DirectoryInfo(this.Path);
    }

    /// <summary>
    /// 结合多个字符串片段生成新的目录路径.
    /// </summary>
    /// <param name="paths">路径片段数组群.</param>
    /// <returns>合并后的目录路径.</returns>
    public DirectoryPath Combine(params string[] paths)
    {
        // 将当前路径作为首项进行合并
        string[] combinedSegments = paths.Prepend(this.Path).ToArray();
        return System.IO.Path.Combine(combinedSegments);
    }

    /// <summary>
    /// 结合多个字符串片段生成新的文件路径.
    /// </summary>
    /// <param name="paths">路径片段数组群.</param>
    /// <returns>合并后的文件路径.</returns>
    public FilePath CombineFilePath(params string[] paths)
    {
        // 将当前路径作为首项进行合并
        string[] combinedSegments = paths.Prepend(this.Path).ToArray();
        return System.IO.Path.Combine(combinedSegments);
    }

    /// <summary>
    /// 获取一个值，该值指示当前目录是否可读.
    /// </summary>
    /// <returns>如果可读则返回 true.</returns>
    public bool IsReadable()
    {
        return FileSystemUtil.IsDirectoryReadable(this.Path);
    }

    /// <summary>
    /// 获取一个值，该值指示当前目录是否可写.
    /// </summary>
    /// <returns>如果可写则返回 true.</returns>
    public bool IsWritable()
    {
        return FileSystemUtil.IsDirectoryWritable(this.Path);
    }

    /// <summary>
    /// 如果目录不存在则创建.
    /// </summary>
    /// <returns>当前目录路径实例.</returns>
    public DirectoryPath CreateIfNotExists()
    {
        Directory.CreateDirectory(this.Path);
        return this;
    }

    /// <summary>
    /// 定义从字符串到目录路径的隐式转换.
    /// </summary>
    /// <param name="name">路径名称字符串.</param>
    /// <returns>目录路径对象.</returns>
    [return: NotNullIfNotNull("name")]
    public static implicit operator DirectoryPath?(string? name)
    {
        return name is null ? null : new DirectoryPath(name);
    }

    /// <summary>
    /// 尝试解析字符串为目录路径实例.
    /// </summary>
    /// <param name="inputString">输入的路径字符串.</param>
    /// <param name="directoryPath">输出的目录路径对象.</param>
    /// <returns>如果解析成功返回 true.</returns>
    public static bool TryParse(string inputString, [NotNullWhen(true)] out DirectoryPath? directoryPath)
    {
        try
        {
            directoryPath = new DirectoryPath(inputString);
            return true;
        }
        catch
        {
            directoryPath = null;
            return false;
        }
    }
}
