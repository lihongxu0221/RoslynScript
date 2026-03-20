namespace BgLogger;

/// <summary>
/// 提供用于记录实时生产过程信息的静态日志方法.
/// </summary>
public class LogProduct : IBgLogger
{
    /// <summary>
    /// 将 Trace 级别的日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Trace(string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Trace(format, args);
    }

    /// <summary>
    /// 将带异常信息的 Trace 级别日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Trace(Exception ex, string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Trace(ex, format, args);
    }

    /// <summary>
    /// 将 Trace 级别的异常信息记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    public static void Trace(Exception ex)
    {
        BgLoggerSource.RealtimeProduction.Trace(ex);
    }

    /// <summary>
    /// 将 Debug 级别的日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Debug(string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Debug(format, args);
    }

    /// <summary>
    /// 将带异常信息的 Debug 级别日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Debug(Exception ex, string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Debug(ex, format, args);
    }

    /// <summary>
    /// 将 Debug 级别的异常信息记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    public static void Debug(Exception ex)
    {
        BgLoggerSource.RealtimeProduction.Debug(ex);
    }

    /// <summary>
    /// 将 Info 级别的日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Info(string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Info(format, args);
    }

    /// <summary>
    /// 将带异常信息的 Info 级别日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Info(Exception ex, string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Info(ex, format, args);
    }

    /// <summary>
    /// 将 Info 级别的异常信息记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    public static void Info(Exception ex)
    {
        BgLoggerSource.RealtimeProduction.Info(ex);
    }

    /// <summary>
    /// 将 Warn 级别的日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Warn(string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Warn(format, args);
    }

    /// <summary>
    /// 将带异常信息的 Warn 级别日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    /// <param name="format">带格式项的日志消息.</param>
    /// <param name="args">格式化参数数组.</param>
    public static void Warn(Exception ex, string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Warn(ex, format, args);
    }

    /// <summary>
    /// 将 Warn 级别的异常信息记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    public static void Warn(Exception ex)
    {
        BgLoggerSource.RealtimeProduction.Warn(ex);
    }

    /// <summary>
    /// 将 Error 级别的日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Error(string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Error(format, args);
    }

    /// <summary>
    /// 将带异常信息的 Error 级别日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">异常对象（可选）.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Error(Exception ex, string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Error(ex, format, args);
    }

    /// <summary>
    /// 将 Error 级别的异常信息记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    public static void Error(Exception ex)
    {
        BgLoggerSource.RealtimeProduction.Error(ex);
    }

    /// <summary>
    /// 将 Fatal 级别的日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Fatal(string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Fatal(format, args);
    }

    /// <summary>
    /// 将带异常信息的 Fatal 级别日志记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">异常对象（可选）.</param>
    /// <param name="format">日志消息 或格式化日志消息.</param>
    /// <param name="args">格式化参数.</param>
    public static void Fatal(Exception ex, string format, params object[] args)
    {
        BgLoggerSource.RealtimeProduction.Fatal(ex, format, args);
    }

    /// <summary>
    /// 将 Fatal 级别的异常信息记录到 RealtimeProduction 日志源.
    /// </summary>
    /// <param name="ex">要记录的异常.</param>
    public static void Fatal(Exception ex)
    {
        BgLoggerSource.RealtimeProduction.Fatal(ex);
    }
}