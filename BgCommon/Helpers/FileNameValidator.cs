using System;
using System.IO;
using System.Linq;

namespace BgCommon;

/// <summary>
/// 一个用于校验文件名的实用工具类.
/// </summary>
public static class FileNameValidator
{
    // 定义文件名最大长度的常量，255 是一个在多种文件系统中都比较安全的值
    private const int MaxFileNameLength = 255;

    // Windows 系统保留的设备名称，不区分大小写
    private static readonly string[] ReservedWindowsNames =
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// 校验文件名是否有效且安全.
    /// 此方法会检查非法字符、长度限制、Windows保留名称以及不推荐的结尾字符.
    /// </summary>
    /// <param name="fileName">待校验的文件名（仅文件名，不含路径）.</param>
    /// <param name="errorMessage">当校验失败时，输出具体的错误信息（格式化的字符串）.</param>
    /// <param name="parameters">参数.</param>
    /// <returns>如果文件名合法，返回 true；否则返回 false.</returns>
    public static bool IsValidFileName(this string fileName, out string errorMessage, out object[] parameters)
    {
        errorMessage = string.Empty;
        parameters = Array.Empty<object>();

        // 1. 校验是否为 null 或仅包含空白字符
        if (string.IsNullOrWhiteSpace(fileName))
        {
            errorMessage = "文件名不能为空";
            return false;
        }

        // 校验文件名长度
        if (fileName.Length > MaxFileNameLength)
        {
            errorMessage = "文件名长度不能超过 {MaxLength} 个字符";
            parameters = new object[] { MaxFileNameLength };
            return false;
        }

        // 校验是否包含跨平台通用的非法字符
        // Path.GetInvalidFileNameChars() 会根据当前运行的操作系统返回对应的非法字符集
        char[] invalidChars = Path.GetInvalidFileNameChars();
        var invalidCharsInFileName = fileName.Intersect(invalidChars).ToList();
        if (invalidCharsInFileName.Count > 0)
        {
            errorMessage = "文件名包含非法字符：{InvalidChars}";
            parameters = new object[] { string.Join(" ", invalidCharsInFileName) };
            return false;
        }

        // (Windows 平台特性) 校验文件名是否以点(.)或空格( )结尾
        // 尽管在某些情况下可以创建，但这会导致在资源管理器中出现问题，通常应避免
        if (OperatingSystem.IsWindows())
        {
            if (fileName.EndsWith('.') || fileName.EndsWith(' '))
            {
                errorMessage = "文件名不能以点或空格结尾";
                return false;
            }
        }

        // (Windows 平台特性) 校验文件名是否与系统保留名称冲突
        if (OperatingSystem.IsWindows())
        {
            // 获取不带扩展名的文件名部分，并转换为大写以便不区分大小写比较
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            // 如果文件名（不含扩展名）与任何保留名称完全相同
            if (ReservedWindowsNames.Contains(fileNameWithoutExt, StringComparer.OrdinalIgnoreCase))
            {
                errorMessage = "文件名 '{FileName}' 是一个 Windows 系统保留名称";
                parameters = new object[] { fileNameWithoutExt };
                return false;
            }
        }

        // 所有校验通过
        return true;
    }

    /// <summary>
    /// 安全获取文件名主体.
    /// </summary>
    /// <param name="fileName">文件名或文件路径.</param>
    /// <param name="extension">扩展名(例如：.cs).</param>
    /// <returns>返回安全的文件名主体.</returns>
    public static string ToSafeFileName(this string fileName, string extension)
    {
        // 基础非空检查
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            if (!extension.StartsWith('.'))
            {
                extension = $".{extension}";
            }

            // 提取文件名（去除路径部分，如 c:\a\b\d.cs -> d.cs）
            string pureName = Path.GetFileName(fileName);

            // 如果输入仅包含路径（例如 "C:\a\b\"），pureName 会为空
            if (!string.IsNullOrEmpty(pureName))
            {
                // 处理固定扩展名.
                if (pureName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    // 使用经典的 Substring 代替索引语法糖
                    pureName = pureName.Substring(0, pureName.Length - extension.Length);
                }

                // 过滤非法字符
                // 获取系统定义的非法文件名字符
                char[] invalidChars = Path.GetInvalidFileNameChars();

                // 使用 StringBuilder 构建结果，避免产生大量字符串中间件
                StringBuilder sb = new StringBuilder();
                foreach (char c in pureName)
                {
                    // 如果字符不在非法字符数组中，则添加
                    if (Array.IndexOf(invalidChars, c) < 0)
                    {
                        sb.Append(c);
                    }
                }

                string result = sb.ToString().Trim();

                // 最终检查（如果文件名全是空格或非法字符，过滤后会变成空）
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
        }

        return string.Empty;
    }
}