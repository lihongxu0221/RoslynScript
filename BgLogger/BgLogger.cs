namespace BgLogger;

/// <summary>
/// 提供统一的日志记录静态方法，支持基于 LogSource 枚举或字符串源名的日志输出.
/// </summary>
public static class BgLogger
{
    private static readonly ConcurrentDictionary<string, Logger> Loggers = new();

    /// <summary>
    /// 获取特定源的 NLog Logger 实例.
    /// </summary>
    /// <param name="source">日志源（LogSource 枚举）.</param>
    /// <returns>NLog Logger 实例.</returns>
    private static Logger GetLogger(BgLoggerSource source)
    {
        string keyNames = source.ToString();
        return GetLogger(keyNames);
    }

    /// <summary>
    /// 获取特定源的 NLog Logger 实例. 如果实例不存在，则创建并缓存它.
    /// </summary>
    /// <param name="sourceName">日志源 字符串.</param>
    /// <returns>NLog Logger 实例.</returns>
    private static Logger GetLogger(string sourceName)
    {
        return Loggers.GetOrAdd(sourceName, LogManager.GetLogger(sourceName));
    }

    /// <summary>
    /// 记录 Trace 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Trace(this BgLoggerSource source, string format, params object[] args)
    {
        GetLogger(source)?.Trace(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录带异常信息的 Trace 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Trace(this BgLoggerSource source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Trace(ex, format, args);
    }

    /// <summary>
    /// 记录 Trace 级别的异常信息（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Trace(this BgLoggerSource source, Exception ex)
    {
        GetLogger(source)?.Trace(ex);
    }

    /// <summary>
    /// 记录 Debug 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Debug(this BgLoggerSource source, string format, params object[] args)
    {
        GetLogger(source)?.Debug(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录带异常信息的 Debug 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Debug(this BgLoggerSource source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Debug(ex, format, args);
    }

    /// <summary>
    /// 记录 Debug 级别的异常信息（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Debug(this BgLoggerSource source, Exception ex)
    {
        GetLogger(source)?.Debug(ex);
    }

    /// <summary>
    /// 记录 Info 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Info(this BgLoggerSource source, string format, params object[] args)
    {
        GetLogger(source)?.Info(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录带异常信息的 Info 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Info(this BgLoggerSource source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Info(ex, format, args);
    }

    /// <summary>
    /// 记录 Info 级别的异常信息（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Info(this BgLoggerSource source, Exception ex)
    {
        GetLogger(source)?.Info(ex);
    }

    /// <summary>
    /// 记录 Warn 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Warn(this BgLoggerSource source, string format, params object[] args)
    {
        GetLogger(source)?.Warn(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录带异常信息的 Warn 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Warn(this BgLoggerSource source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Warn(ex, format, args);
    }

    /// <summary>
    /// 记录 Warn 级别的异常信息（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Warn(this BgLoggerSource source, Exception ex)
    {
        GetLogger(source)?.Warn(ex);
    }

    /// <summary>
    /// 记录 Error 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Error(this BgLoggerSource source, string format, params object[] args)
    {
        GetLogger(source)?.Error(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录 Error 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">异常对象（可选）.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Error(this BgLoggerSource source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Error(ex, format, args);
    }

    /// <summary>
    /// 记录 Error 级别的异常信息（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Error(this BgLoggerSource source, Exception ex)
    {
        GetLogger(source)?.Error(ex);
    }

    /// <summary>
    /// 记录 Fatal 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Fatal(this BgLoggerSource source, string format, params object[] args)
    {
        GetLogger(source)?.Fatal(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录 Fatal 级别日志（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">异常对象（可选）.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Fatal(this BgLoggerSource source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Fatal(ex, format, args);
    }

    /// <summary>
    /// 记录 Fatal 级别的异常信息（基于 LogSource 枚举）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Fatal(this BgLoggerSource source, Exception ex)
    {
        GetLogger(source)?.Fatal(ex);
    }

    /// <summary>
    /// 记录 Trace 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Trace(string source, string format, params object[] args)
    {
        GetLogger(source)?.Trace(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录带异常信息的 Trace 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Trace(string source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Trace(ex, format, args);
    }

    /// <summary>
    /// 记录 Trace 级别的异常信息（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Trace(string source, Exception ex)
    {
        GetLogger(source)?.Trace(ex);
    }

    /// <summary>
    /// 记录 Debug 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Debug(string source, string format, params object[] args)
    {
        GetLogger(source)?.Debug(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录带异常信息的 Debug 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Debug(string source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Debug(ex, format, args);
    }

    /// <summary>
    /// 记录 Debug 级别的异常信息（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Debug(string source, Exception ex)
    {
        GetLogger(source)?.Debug(ex);
    }

    /// <summary>
    /// 记录 Info 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Info(string source, string format, params object[] args)
    {
        GetLogger(source)?.Info(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录带异常信息的 Info 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Info(string source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Info(ex, format, args);
    }

    /// <summary>
    /// 记录 Info 级别的异常信息（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Info(string source, Exception ex)
    {
        GetLogger(source)?.Info(ex);
    }

    /// <summary>
    /// 记录 Warn 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Warn(string source, string format, params object[] args)
    {
        GetLogger(source)?.Warn(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录带异常信息的 Warn 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Warn(string source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Warn(ex, format, args);
    }

    /// <summary>
    /// 记录 Warn 级别的异常信息（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Warn(string source, Exception ex)
    {
        GetLogger(source)?.Warn(ex);
    }

    /// <summary>
    /// 记录 Error 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Error(string source, string format, params object[] args)
    {
        GetLogger(source)?.Error(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录 Error 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">异常对象（可选）.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Error(string source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Error(ex, format, args);
    }

    /// <summary>
    /// 记录 Error 级别的异常信息（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Error(string source, Exception ex)
    {
        GetLogger(source)?.Error(ex);
    }

    /// <summary>
    /// 记录 Fatal 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Fatal(string source, string format, params object[] args)
    {
        GetLogger(source)?.Fatal(CultureInfo.CurrentCulture, format, args);
    }

    /// <summary>
    /// 记录 Fatal 级别日志（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源.</param>
    /// <param name="ex">异常对象（可选）.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Fatal(string source, Exception ex, string format, params object[] args)
    {
        GetLogger(source)?.Fatal(ex, format, args);
    }

    /// <summary>
    /// 记录 Fatal 级别的异常信息（基于字符串源名）.
    /// </summary>
    /// <param name="source">日志源名称.</param>
    /// <param name="ex">要记录的异常.</param>
    public static void Fatal(string source, Exception ex)
    {
        GetLogger(source)?.Fatal(ex);
    }
}