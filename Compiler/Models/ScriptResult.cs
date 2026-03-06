using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;

namespace BgCommon.Script.Compiler.Models;

/// <summary>
/// 表示脚本执行结果的类，包含成功状态、返回消息、结果对象及可能的异常信息.
/// </summary>
public class ScriptResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptResult"/> class.
    /// </summary>
    internal ScriptResult()
    {
        this.Outputs = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptResult"/> class.
    /// </summary>
    /// <param name="success">指示脚本执行是否成功的布尔值.</param>
    /// <param name="message">执行结果的描述信息.</param>
    /// <param name="result">脚本执行返回的可选结果对象.</param>
    /// <param name="exception">脚本执行过程中产生的可选异常信息.</param>
    /// <param name="inputs">输输入参数池.</param>
    /// <param name="outputs">输出参数池.</param>
    /// <param name="scriptFilePath">脚本路径.</param>
    /// <param name="scriptCode">脚本内容.</param>
    /// <param name="targetType">脚本对应的类型名称.</param>
    /// <param name="targetMethod">脚本待执行的方法名称.</param>
    public ScriptResult(
        bool success,
        string message,
        object? result = null,
        Exception? exception = null,
        object? inputs = null,
        IDictionary<string, object?>? outputs = null,
        string? scriptFilePath = null,
        string? scriptCode = null,
        string? targetType = null,
        string? targetMethod = null)
    {
        // 验证消息参数是否为空.
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        // 为只读属性赋值，显式使用 this 标识.
        this.Success = success;
        this.Message = message;
        this.Result = result;
        this.Exception = exception;
        this.Inputs = inputs;

        // 将输出参数池包装为只读字典
        this.Outputs = outputs != null
            ? new ReadOnlyDictionary<string, object?>(outputs)
            : new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

        this.ScriptFilePath = scriptFilePath ?? string.Empty;
        this.ScriptCode = scriptCode ?? string.Empty;
        this.TargetType = targetType ?? string.Empty;
        this.TargetMethod = targetMethod ?? string.Empty;
        this.Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Gets a value indicating whether 脚本执行是否成功.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets 脚本执行过程中产生的异常.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets 执行结果的描述消息.
    /// </summary>
    public string Message { get; } = string.Empty;

    /// <summary>
    /// Gets 脚本执行返回的结果对象.
    /// </summary>
    public object? Result { get; }

    /// <summary>
    /// Gets 存储执行时的输入参数池（通常是 globals.Data）. 这对于调试和日志记录非常有用，可以让调用者了解脚本执行时的上下文信息.
    /// </summary>
    public object? Inputs { get; }

    /// <summary>
    /// Gets 脚本执行产生的输出参数池（包含 ref/out 参数或手动添加的结果）.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Outputs { get; }

    /// <summary>
    /// Gets 脚本文件的物理路径.
    /// </summary>
    public string ScriptFilePath { get; }

    /// <summary>
    /// Gets 运行时的脚本内容快照（便于排查代码变更后的历史问题）.
    /// </summary>
    public string ScriptCode { get; }

    /// <summary>
    /// Gets 运行的目标类名.
    /// </summary>
    public string TargetType { get; }

    /// <summary>
    /// Gets 运行的目标方法名.
    /// </summary>
    public string TargetMethod { get; }

    /// <summary>
    /// Gets 结果生成的时间戳.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets a value indicating whether 判断是否为编译阶段的错误（语法/语义）.
    /// </summary>
    public bool IsCompilationError => this.Exception is ScriptCompilationException;

    /// <summary>
    /// Gets a value indicating whether 判断是否为框架/宿主层面的异常（非脚本代码逻辑问题）.
    /// </summary>
    public bool IsFrameworkError => !Success && !IsCompilationError && Exception != null && !(Exception is OperationCanceledException);

    /// <summary>
    /// 获取指定类型的输出参数.
    /// </summary>
    /// <typeparam name="T">指定类型.</typeparam>
    /// <param name="parameterName">参数名称.</param>
    /// <returns>指定类型的输出参数值.</returns>
    public T? GetOutput<T>(string parameterName)
    {
        if (Outputs.TryGetValue(parameterName, out var value) && value is T tValue)
        {
            return tValue;
        }

        return default;
    }

    /// <summary>
    /// Gets 详细的可视化错误信息.
    /// 根据异常类型自动解析：编译错误展示诊断详情，运行时错误展示堆栈.
    /// </summary>
    public string ExceptionDetail
    {
        get
        {
            if (this.Exception == null)
            {
                return string.Empty;
            }

            // 情况 1: 处理 Roslyn 编译异常
            if (this.Exception is ScriptCompilationException compileEx)
            {
                var errorLines = compileEx.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d =>
                    {
                        var lineSpan = d.Location.GetLineSpan();

                        // 格式: [ErrorID] 消息内容 (位于 行:列)
                        return $"[{d.Id}] {d.GetMessage()} (at Line: {lineSpan.StartLinePosition.Line + 1}, Col: {lineSpan.StartLinePosition.Character})";
                    });

                return $"编译失败详情:{Environment.NewLine}{string.Join(Environment.NewLine, errorLines)}";
            }

            // 情况 2: 处理常规运行时异常
            // 包含 Message 和 StackTrace (StackSource)
            return $"运行异常: {this.Exception.Message}{Environment.NewLine}堆栈轨迹:{Environment.NewLine}{this.Exception.StackTrace}";
        }
    }

    /// <summary>
    /// 获取格式化后的完整错误报告，常用于 UI 弹窗或日志写入.
    /// </summary>
    /// <returns>返回 格式化异常信息.</returns>
    public string GetFormattedError()
    {
        if (this.Success)
        {
            return "Success";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"--- 脚本执行错误报告 ---");
        sb.AppendLine($"时间: {this.Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"文件: {this.ScriptFilePath}");
        if (!string.IsNullOrEmpty(this.TargetType))
        {
            sb.AppendLine($"入口: {this.TargetType}.{this.TargetMethod}");
        }

        sb.AppendLine($"概述: {this.Message}");
        sb.AppendLine($"详情: {this.ExceptionDetail}");

        return sb.ToString();
    }
}