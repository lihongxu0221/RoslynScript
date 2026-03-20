namespace BgCommon.Core.Models;

/// <summary>
/// 操作结果类，用于封装各类操作的执行结果信息.
/// </summary>
public class OperateResult
{
    /// <summary>
    /// Gets a value indicating whether gets 表示操作是否成功执行.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets  操作结果的状态码，用于更细致地表示操作结果类型.
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// Gets 操作结果的消息描述，通常用于展示给用户或记录日志.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets 操作返回的具体数据结果，可为空.
    /// </summary>
    public object? Result { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperateResult"/> class.
    /// </summary>
    /// <param name="success">操作是否成功.</param>
    /// <param name="code">状态码.</param>
    /// <param name="message">消息描述.</param>
    /// <param name="result">返回的数据结果.</param>
    protected OperateResult(bool success, int code, string message, object? result)
    {
        this.Success = success;
        this.Code = code;
        this.Message = message;
        this.Result = result;
    }

    /// <summary>
    /// 创建一个表示操作成功的结果对象.
    /// </summary>
    /// <param name="code">成功状态码.</param>
    /// <param name="message">成功消息描述.</param>
    /// <param name="result">返回的数据结果（可选）.</param>
    /// <returns>操作成功的OperateResult实例.</returns>
    public static OperateResult ToSuccess(int code, string message, object? result = null)
        => new OperateResult(true, code, message, result);

    /// <summary>
    /// 创建一个表示操作失败的结果对象（使用默认失败状态码-1.
    /// </summary>
    /// <param name="message">失败消息描述.</param>
    /// <returns>操作失败的OperateResult实例.</returns>
    public static OperateResult ToFail(string message)
        => ToFail(-1, message);

    /// <summary>
    /// 创建一个表示操作失败的结果对象（指定失败状态码.
    /// </summary>
    /// <param name="code">失败状态码.</param>
    /// <param name="message">失败消息描述.</param>
    /// <returns>操作失败的OperateResult实例.</returns>
    public static OperateResult ToFail(int code, string message)
        => ToFail(code, message, null);

    /// <summary>
    /// 创建一个表示操作失败的结果对象（使用默认失败状态码-1.
    /// </summary>
    /// <param name="message">失败消息描述.</param>
    /// <param name="result">返回的附加数据（可选）.</param>
    /// <returns>操作失败的OperateResult实例.</returns>
    public static OperateResult ToFail(string message, object? result)
        => ToFail(-1, message, result);

    /// <summary>
    /// 创建一个表示操作失败的结果对象（指定失败状态码.
    /// </summary>
    /// <param name="code">失败状态码.</param>
    /// <param name="message">失败消息描述.</param>
    /// <param name="result">返回的附加数据（可选）.</param>
    /// <returns>操作失败的OperateResult实例.</returns>
    public static OperateResult ToFail(int code, string message, object? result)
        => new OperateResult(false, code, message, result);
}

/// <summary>
/// 操作结果类，用于封装各类操作的执行结果信息.
/// </summary>
/// <typeparam name="T">返回结果类型.</typeparam>
public class OperateResult<T> : OperateResult
{
    /// <summary>
    /// Gets 操作返回的具体数据结果，可为空.
    /// </summary>
    public new T? Result { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperateResult{T}"/> class.
    /// </summary>
    /// <param name="success">操作是否成功.</param>
    /// <param name="code">状态码.</param>
    /// <param name="message">消息描述.</param>
    /// <param name="result">返回的数据结果.</param>
    protected OperateResult(bool success, int code, string message, T? result)
        : base(success, code, message, null)
    {
        this.Result = result;
    }

    /// <summary>
    /// 创建一个表示操作成功的结果对象.
    /// </summary>
    /// <param name="code">成功状态码.</param>
    /// <param name="message">成功消息描述.</param>
    /// <returns>操作成功的OperateResult实例.</returns>
    public static OperateResult<T> ToSuccess(int code, string message)
        => ToSuccess(code, message, default);

    /// <summary>
    /// 创建一个表示操作成功的结果对象.
    /// </summary>
    /// <param name="code">成功状态码.</param>
    /// <param name="message">成功消息描述.</param>
    /// <param name="result">返回的数据结果（可选）.</param>
    /// <returns>操作成功的OperateResult实例.</returns>
    public static OperateResult<T> ToSuccess(int code, string message, T? result)
        => new OperateResult<T>(true, code, message, result);

    /// <summary>
    /// 创建一个表示操作失败的结果对象（使用默认失败状态码-1.
    /// </summary>
    /// <param name="code">失败状态码.</param>
    /// <param name="message">失败消息描述.</param>
    /// <returns>操作失败的OperateResult实例.</returns>
    public static new OperateResult<T> ToFail(int code, string message)
        => ToFail(code, message, default);

    /// <summary>
    /// 创建一个表示操作失败的结果对象（使用默认失败状态码-1.
    /// </summary>
    /// <param name="message">失败消息描述.</param>
    /// <param name="result">返回的附加数据（可选）.</param>
    /// <returns>操作失败的OperateResult实例.</returns>
    public static OperateResult<T> ToFail(string message, T? result)
        => ToFail(-1, message, result);

    /// <summary>
    /// 创建一个表示操作失败的结果对象（指定失败状态码.
    /// </summary>
    /// <param name="code">失败状态码.</param>
    /// <param name="message">失败消息描述.</param>
    /// <param name="result">返回的附加数据（可选）.</param>
    /// <returns>操作失败的OperateResult实例.</returns>
    public static OperateResult<T> ToFail(int code, string message, T? result)
        => new OperateResult<T>(false, code, message, result);
}
