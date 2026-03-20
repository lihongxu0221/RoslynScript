using System.Runtime.InteropServices;
using System.Text;

namespace BgCommon;

/// <summary>
/// 提供系统环境信息、路径处理及常用操作的公共工具类.
/// </summary>
public static class Common
{
    /// <summary>
    /// Gets 当前应用程序基路径.
    /// </summary>
    public static string ApplicationBaseDirectory => AppContext.BaseDirectory;

    /// <summary>
    /// Gets 当前操作系统的换行符.
    /// </summary>
    public static string Line => System.Environment.NewLine;

    /// <summary>
    /// Gets a value indicating whether 是否为 Linux 操作系统.
    /// </summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets a value indicating whether 是否为 Windows 操作系统.
    /// </summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// 获取指定类型的实际类型，如果是可空类型则返回其底层基础类型.
    /// </summary>
    /// <typeparam name="T">要获取的目标类型.</typeparam>
    /// <returns>返回处理后的类型对象.</returns>
    public static Type GetType<T>()
    {
        // 调用重载方法处理当前类型
        return GetType(typeof(T));
    }

    /// <summary>
    /// 获取指定类型的实际类型，如果是可空类型则返回其底层基础类型.
    /// </summary>
    /// <param name="type">需要处理的原始类型.</param>
    /// <returns>返回处理后的类型对象.</returns>
    public static Type GetType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        // 如果是 Nullable 类型，则获取其基础类型，否则返回原类型
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>
    /// 根据相对路径获取绝对物理路径.
    /// </summary>
    /// <param name="relativePath">相对路径字符串，例如: "test/a.txt".</param>
    /// <param name="basePath">基路径，若为空则默认为应用程序基目录.</param>
    /// <returns>返回合并后的绝对路径.</returns>
    public static string GetPhysicalPath(string relativePath, string? basePath = null)
    {
        ArgumentNullException.ThrowIfNull(relativePath, nameof(relativePath));

        // 移除路径起始处的波浪号或斜杠
        if (relativePath.StartsWith('~'))
        {
            relativePath = relativePath.TrimStart('~');
        }

        if (relativePath.StartsWith('/'))
        {
            relativePath = relativePath.TrimStart('/');
        }

        if (relativePath.StartsWith('\\'))
        {
            relativePath = relativePath.TrimStart('\\');
        }

        // 确定基路径
        basePath ??= ApplicationBaseDirectory;

        // 合并路径并返回
        return Path.Combine(basePath, relativePath);
    }

    /// <summary>
    /// 连接多个路径片段.
    /// </summary>
    /// <param name="paths">路径片段列表.</param>
    /// <returns>连接后的完整路径.</returns>
    public static string JoinPath(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths, nameof(paths));

        // 调用 UrlHelper 处理路径合并逻辑
        return UrlHelper.JoinPath(paths);
    }

    /// <summary>
    /// 获取当前工作目录路径.
    /// </summary>
    /// <returns>当前目录的完整路径字符串.</returns>
    public static string GetCurrentDirectory()
    {
        // 调用系统接口获取当前目录
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// 获取当前目录向上指定深度的上级目录路径.
    /// </summary>
    /// <param name="depth">向上钻取的深度，默认为 1.</param>
    /// <returns>返回父级目录的路径.</returns>
    public static string GetParentDirectory(int depth = 1)
    {
        string currentPath = Directory.GetCurrentDirectory();

        // 循环向上查找父目录
        for (int i = 0; i < depth; i++)
        {
            DirectoryInfo? parentInfo = Directory.GetParent(currentPath);
            if (parentInfo != null && parentInfo.Exists)
            {
                currentPath = parentInfo.FullName;
            }
        }

        return currentPath;
    }

    /// <summary>
    /// 异步打开文件选择对话框.
    /// </summary>
    /// <param name="isMultiselect">是否允许选择多个文件.</param>
    /// <param name="filters">文件后缀过滤条件，例如 "jpg|png".</param>
    /// <returns>返回选中的文件路径数组，若取消则返回 null.</returns>
    public static async Task<string[]?> OnOpenFileDialogAsync(bool isMultiselect, string? filters)
    {
        try
        {
            List<string> filterList = new List<string>();

            // 构建文件过滤器字符串
            if (string.IsNullOrEmpty(filters))
            {
                filterList.Add("*");
            }
            else
            {
                string[] extensionArray = filters.Split('|');

                // 添加“所有支持文件”选项
                filterList.Add(string.Format("All files({0})|{0}", string.Join(";", extensionArray.Select(ft => $"*.{ft}"))));

                // 分别为每个后缀添加过滤选项
                filterList.AddRange(extensionArray.Select(ft => $"{ft} files (*.{ft})|*.{ft}"));
            }

            // 创建对话框实例
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = string.Join("|", filterList),
                Multiselect = isMultiselect,
            };

            // 清理临时列表
            filterList.Clear();

            // 显示对话框（注意：ShowDialog 是阻塞调用的）
            bool? dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == true)
            {
                return await Task.FromResult(openFileDialog.FileNames);
            }
        }
        catch (Exception ex)
        {
            // 捕获异常并返回异常任务
            return await Task.FromException<string[]?>(ex);
        }

        return await Task.FromResult<string[]?>(null);
    }

    /// <summary>
    /// 异步打开文件夹选择对话框.
    /// </summary>
    /// <param name="isMultiselect">是否允许选择多个文件夹.</param>
    /// <param name="initialDirectory">对话框打开的初始目录.</param>
    /// <returns>返回选中的文件夹路径数组.</returns>
    public static async Task<string[]?> OnOpenFolderDialogAsync(bool isMultiselect, string? initialDirectory = null)
    {
        try
        {
            // 创建选择文件夹对话框实例
            var folderDialog = new Microsoft.Win32.OpenFolderDialog
            {
                Multiselect = isMultiselect,
                Title = string.Empty,
            };

            // 设置初始目录
            if (!string.IsNullOrEmpty(initialDirectory))
            {
                folderDialog.InitialDirectory = initialDirectory;
            }

            // 显示对话框
            bool? dialogResult = folderDialog.ShowDialog();
            if (dialogResult == true)
            {
                return await Task.FromResult(folderDialog.FolderNames);
            }

            return await Task.FromResult<string[]?>(null);
        }
        catch (Exception ex)
        {
            // 返回异常状态的任务
            return await Task.FromException<string[]?>(ex);
        }
    }
}