namespace BgCommon.Helpers;

/// <summary>
/// INI 文件操作类，提供基于 Win32 API 的配置文件读写功能.
/// </summary>
public class IniFile
{
    /// <summary>
    /// Ini 文件路径字段.
    /// </summary>
    private readonly string filePath = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniFile"/> class.
    /// </summary>
    /// <param name="filePath">文件路径.</param>
    public IniFile(string filePath)
    {
        // 显式引用非静态字段
        this.filePath = filePath;

        // 获取并创建文件夹路径
        string? directoryPath = Path.GetDirectoryName(this.filePath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Gets Ini 文件路径.
    /// </summary>
    public string FilePath => this.filePath;

    /// <summary>
    /// 写入 INI 文件.
    /// </summary>
    /// <param name="section">配置文件的节名.</param>
    /// <param name="key">具体的键名.</param>
    /// <param name="value">要写入的字符串值.</param>
    public void Write(string section, string key, string value)
    {
        // 调用静态方法写入
        Write(this.filePath, section, key, value);
    }

    /// <summary>
    /// 写入 INI 文件数组值.
    /// </summary>
    /// <param name="section">配置文件的节名.</param>
    /// <param name="key">具体的键名.</param>
    /// <param name="values">要写入的数组对象.</param>
    public void WriteArray(string section, string key, Array values)
    {
        // 调用静态方法写入数组
        WriteArray(this.filePath, section, key, values);
    }

    /// <summary>
    /// 读取 INI 文件中的字符串.
    /// </summary>
    /// <param name="section">配置文件的节名.</param>
    /// <param name="key">具体的键名.</param>
    /// <param name="defaultValue">默认返回值.</param>
    /// <returns>返回读取到的字符串值.</returns>
    public string Read(string section, string key, string defaultValue = "")
    {
        // 调用静态方法读取
        return Read(this.filePath, section, key, defaultValue);
    }

    /// <summary>
    /// 尝试按指定类型读取单个值.
    /// </summary>
    /// <typeparam name="T">目标数据类型.</typeparam>
    /// <param name="section">配置文件的节名.</param>
    /// <param name="key">具体的键名.</param>
    /// <param name="value">输出读取到的转换后的值.</param>
    /// <returns>成功返回 true，否则返回 false.</returns>
    public bool TryRead<T>(string section, string key, out T? value)
    {
        // 调用静态泛型方法
        return TryRead<T>(this.filePath, section, key, out value);
    }

    /// <summary>
    /// 尝试按指定类型读取单个值.
    /// </summary>
    /// <param name="section">配置文件的节名.</param>
    /// <param name="key">具体的键名.</param>
    /// <param name="value">输出读取到的对象.</param>
    /// <param name="valueType">目标转换类型.</param>
    /// <returns>成功返回 true，否则返回 false.</returns>
    public bool TryRead(string section, string key, out object? value, Type valueType)
    {
        // 调用静态方法读取
        return TryRead(this.filePath, section, key, out value, valueType);
    }

    /// <summary>
    /// 尝试按指定类型读取数组.
    /// </summary>
    /// <param name="section">配置文件的节名.</param>
    /// <param name="key">具体的键名.</param>
    /// <param name="value">输出读取到的数组对象.</param>
    /// <param name="valueType">数组元素的转换类型.</param>
    /// <returns>成功返回 true，否则返回 false.</returns>
    public bool TryReadArray(string section, string key, out object? value, Type valueType)
    {
        value = null;
        string rawString = Read(section, key, string.Empty);
        if (!string.IsNullOrEmpty(rawString))
        {
            // 处理以逗号分隔的数组字符串
            string[] rawItems = rawString.Split(',');
            object[] parsedArray = new object[rawItems.Length];

            for (int i = 0; i < rawItems.Length; i++)
            {
                if (!TryParse(rawItems[i], out object? parsedElement, valueType))
                {
                    Array.Clear(parsedArray, 0, parsedArray.Length);
                    return false;
                }

                parsedArray[i] = parsedElement!;
            }

            value = parsedArray;
        }

        return true;
    }

    /// <summary>
    /// 静态读取 INI 文件方法（动态扩容缓冲区）.
    /// </summary>
    /// <param name="filePath">INI 文件路径.</param>
    /// <param name="section">节名.</param>
    /// <param name="key">键名.</param>
    /// <param name="defaultValue">默认值.</param>
    /// <param name="initialBufferSize">初始缓冲区大小（默认256）.</param>
    /// <param name="maxBufferSize">最大缓冲区大小（防止无限扩容，默认8192）.</param>
    /// <returns>完整的配置值.</returns>
    /// <exception cref="InvalidOperationException">超出最大缓冲区大小时抛出.</exception>
    public static string Read(
        string filePath,
        string section,
        string key,
        string defaultValue,
        int initialBufferSize = 256,
        int maxBufferSize = 8192)
    {
        // 校验参数合理性
        if (initialBufferSize <= 0 || maxBufferSize < initialBufferSize)
        {
            throw new ArgumentException("最大缓冲区大小必须大于初始缓冲区大小，且初始值需大于0");
        }

        int currentBufferSize = initialBufferSize;
        char[] buffer;
        uint bytesRead;

        do
        {
            // 初始化当前大小的缓冲区
            buffer = new char[currentBufferSize];

            // 调用 API 读取值
            bytesRead = NativeMethods.GetPrivateProfileString(
                section,
                key,
                defaultValue,
                buffer,
                (uint)currentBufferSize,
                filePath);

            // 核心判断：如果读取的字符数 = 缓冲区大小-1，说明内容被截断（API 会留1位给终止符）
            bool isTruncated = bytesRead == currentBufferSize - 1;

            if (!isTruncated)
            {
                // 未截断，返回完整值
                return new string(buffer, 0, (int)bytesRead);
            }

            // 截断了，扩容缓冲区（翻倍，或直接到最大值）
            currentBufferSize *= 2;
            if (currentBufferSize > maxBufferSize)
            {
                throw new InvalidOperationException(
                    $"INI 配置值过长，超过最大缓冲区大小 {maxBufferSize} 字节。" +
                    $"请检查 [{section}] {key} 的值，或调整 maxBufferSize 参数。");
            }
        }
        while (true); // 循环直到读取完整或超出最大限制
    }

    /// <summary>
    /// 静态写入 INI 文件方法.
    /// </summary>
    /// <param name="filePath">文件路径.</param>
    /// <param name="section">节名.</param>
    /// <param name="key">键名.</param>
    /// <param name="value">字符串值.</param>
    /// <returns>写入结果.</returns>
    public static bool Write(string filePath, string section, string key, string value)
    {
        return NativeMethods.WritePrivateProfileString(section, key, value, filePath);
    }

    /// <summary>
    /// 静态写入 INI 文件数组方法.
    /// </summary>
    /// <param name="filePath">文件路径.</param>
    /// <param name="section">节名.</param>
    /// <param name="key">键名.</param>
    /// <param name="values">数组对象.</param>
    public static void WriteArray(string filePath, string section, string key, Array values)
    {
        string[] stringItems = new string[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            string? convertedString = string.Empty;
            object? elementValue = values.GetValue(i);

            if (elementValue != null)
            {
                _ = TryParse(elementValue, out convertedString, elementValue.GetType());
            }

            stringItems[i] = convertedString!;
        }

        // 将数组组合成逗号分隔的字符串存入
        NativeMethods.WritePrivateProfileString(section, key, string.Join(",", stringItems), filePath);
    }

    /// <summary>
    /// 静态尝试读取指定类型方法.
    /// </summary>
    /// <typeparam name="T">目标类型.</typeparam>
    /// <param name="filePath">文件路径.</param>
    /// <param name="section">节名.</param>
    /// <param name="key">键名.</param>
    /// <param name="value">输出值.</param>
    /// <returns>读取结果.</returns>
    public static bool TryRead<T>(string filePath, string section, string key, out T? value)
    {
        value = default;
        string rawString = Read(filePath, section, key, string.Empty);

        // 尝试解析字符串为目标对象
        if (TryParse(rawString, out object? parsedResult, typeof(T)) && parsedResult != null)
        {
            value = (T)parsedResult;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 静态尝试读取指定类型方法.
    /// </summary>
    /// <param name="filePath">文件路径.</param>
    /// <param name="section">节名.</param>
    /// <param name="key">键名.</param>
    /// <param name="value">输出值.</param>
    /// <param name="valueType">目标类型.</param>
    /// <returns>读取结果.</returns>
    public static bool TryRead(string filePath, string section, string key, out object? value, Type valueType)
    {
        string rawString = Read(filePath, section, key, string.Empty);
        return TryParse(rawString, out value, valueType);
    }

    /// <summary>
    /// 内部解析方法：从字符串解析为指定类型的对象.
    /// </summary>
    private static bool TryParse(string content, out object? value, Type valueType)
    {
        value = null;

        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        // 根据类型分支进行解析
        if (valueType == typeof(string))
        {
            value = content;
            return true;
        }

        if (valueType == typeof(byte))
        {
            if (byte.TryParse(content, out byte result))
            {
                value = result;
                return true;
            }
        }

        if (valueType == typeof(short))
        {
            if (short.TryParse(content, out short result))
            {
                value = result;
                return true;
            }
        }

        if (valueType == typeof(int))
        {
            if (int.TryParse(content, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                value = result;
                return true;
            }
        }

        if (valueType == typeof(long))
        {
            if (long.TryParse(content, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
            {
                value = result;
                return true;
            }
        }

        if (valueType == typeof(float))
        {
            if (float.TryParse(content, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            {
                value = result;
                return true;
            }
        }

        if (valueType == typeof(double))
        {
            if (double.TryParse(content, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                value = result;
                return true;
            }
        }

        if (valueType == typeof(DateTime))
        {
            if (DateTime.TryParse(content, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                value = result;
                return true;
            }
        }

        if (valueType.IsEnum)
        {
            if (Enum.TryParse(valueType, content, out object? result))
            {
                value = result;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 内部解析方法：将对象序列化为 INI 存储字符串.
    /// </summary>
    private static bool TryParse(object? value, out string? content, Type valueType)
    {
        content = string.Empty;

        if (value == null)
        {
            return false;
        }

        if (valueType == typeof(float) || valueType == typeof(double))
        {
            // 确保小数点始终为 "."
            content = string.Format(CultureInfo.InvariantCulture, "{0}", value);
            return true;
        }

        // 处理时间格式，带毫秒
        if (valueType == typeof(DateTime))
        {
            DateTime dateTimeValue = (DateTime)Convert.ChangeType(value, typeof(DateTime));
            content = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            return true;
        }

        // 处理基础数据类型和枚举
        if (valueType == typeof(string) ||
            valueType == typeof(byte) ||
            valueType == typeof(short) ||
            valueType == typeof(int) ||
            valueType == typeof(long) ||
            valueType.IsEnum)
        {
            content = Convert.ToString(value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 包含 Win32 原生操作方法的内部类.
    /// </summary>
    private static class NativeMethods
    {
        /// <summary>
        /// 写入 INI 文件的 Win32 API.
        /// </summary>
        /// <param name="section">节名.</param>
        /// <param name="key">键名.</param>
        /// <param name="value">值.</param>
        /// <param name="filePath">文件全路径.</param>
        /// <returns>操作结果.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);

        /// <summary>
        /// 读取 INI 文件的 Win32 API.
        /// </summary>
        /// <param name="section">节名.</param>
        /// <param name="key">键名.</param>
        /// <param name="defaultValue">未找到时的默认值.</param>
        /// <param name="lpReturnedString">输出缓冲区.</param>
        /// <param name="size">缓冲区大小.</param>
        /// <param name="filePath">文件全路径.</param>
        /// <returns>读取到的字符数.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint GetPrivateProfileString(
            string section,
            string key,
            string defaultValue,
            [Out] char[] lpReturnedString,
            uint size,
            string filePath);
    }
}