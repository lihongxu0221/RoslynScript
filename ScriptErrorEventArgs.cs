using Microsoft.CodeAnalysis;

namespace BgCommon.Script;

/// <summary>
/// 表示脚本错误的来源枚举.
/// </summary>
public enum ScriptErrorSource
{
    /// <summary>
    /// 加载阶段错误.
    /// </summary>
    Loading,

    /// <summary>
    /// 编译阶段错误.
    /// </summary>
    Compilation,

    /// <summary>
    /// 执行阶段错误.
    /// </summary>
    Execution,

    /// <summary>
    /// 保存阶段错误.
    /// </summary>
    Saving,

    /// <summary>
    /// 文件操作错误（如重命名、删除等）.
    /// </summary>
    FileOperation,
}

/// <summary>
/// 脚本文件操作类型枚举.
/// </summary>
public enum ScriptFileAction
{
    /// <summary>
    /// 从磁盘加载已有文件.
    /// </summary>
    Loaded,

    /// <summary>
    /// 脚本保存（覆盖原有文件）.
    /// </summary>
    Saved,

    /// <summary>
    /// 从模板初始化并首次创建文件.
    /// </summary>
    CreatedFromTemplate,

    /// <summary>
    /// 脚本重命名（物理路径发生了移动/更名）.
    /// </summary>
    Renamed,

    /// <summary>
    /// 脚本另存为（创建了新的副本）.
    /// </summary>
    SavedAs,

    /// <summary>
    /// 脚本文件被物理删除.
    /// </summary>
    Deleted,
}

/// <summary>
/// 脚本错误事件参数，用于传递脚本在各阶段发生的异常信息.
/// </summary>
public class ScriptErrorEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptErrorEventArgs"/> class.
    /// </summary>
    /// <param name="source">错误的来源.</param>
    /// <param name="exception">发生的异常对象.</param>
    /// <param name="message">错误描述信息文本.</param>
    public ScriptErrorEventArgs(ScriptErrorSource source, Exception? exception, string message)
    {
        // 验证错误消息是否为空.
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        // 为只读属性赋值.
        this.Source = source;
        this.Exception = exception;
        this.Message = message;
    }

    /// <summary>
    /// Gets 脚本错误的来源.
    /// </summary>
    public ScriptErrorSource Source { get; }

    /// <summary>
    /// Gets 脚本执行过程中产生的异常信息.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets 错误的详细描述信息.
    /// </summary>
    public string Message { get; }
}

/// <summary>
/// 脚本编译结果事件参数，用于描述 Roslyn 编译后的状态和诊断信息.
/// </summary>
public class ScriptCompilationEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationEventArgs"/> class.
    /// </summary>
    /// <param name="isSuccess">编译是否成功.</param>
    /// <param name="diagnostics">编译产生的诊断（错误、警告）集合.</param>
    /// <param name="duration">编译消耗的时长.</param>
    public ScriptCompilationEventArgs(bool isSuccess, IReadOnlyList<Diagnostic> diagnostics, TimeSpan duration)
    {
        // 验证诊断信息列表是否为空.
        ArgumentNullException.ThrowIfNull(diagnostics, nameof(diagnostics));

        this.IsSuccess = isSuccess;
        this.Diagnostics = diagnostics;
        this.CompilationDuration = duration;
    }

    /// <summary>
    /// Gets a value indicating whether 脚本编译是否成功.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets 编译过程中产生的诊断信息集合.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets 编译过程所消耗的时长.
    /// </summary>
    public TimeSpan CompilationDuration { get; }
}

/// <summary>
/// 脚本执行结果事件参数，包含执行后的返回值、耗时及异常信息.
/// </summary>
public class ScriptExecutionEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionEventArgs"/> class.
    /// </summary>
    /// <param name="result">脚本执行的返回对象.</param>
    /// <param name="duration">脚本执行的时长.</param>
    /// <param name="exception">执行过程中产生的异常（如果有）.</param>
    public ScriptExecutionEventArgs(object? result, TimeSpan duration, Exception? exception = null)
    {
        this.Result = result;
        this.ExecutionDuration = duration;
        this.Exception = exception;
    }

    /// <summary>
    /// Gets 脚本执行后的返回值.
    /// </summary>
    public object? Result { get; }

    /// <summary>
    /// Gets 脚本执行过程所消耗的时长.
    /// </summary>
    public TimeSpan ExecutionDuration { get; }

    /// <summary>
    /// Gets 脚本执行过程中抛出的异常对象.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets a value indicating whether 脚本执行过程中是否产生了错误.
    /// </summary>
    public bool HasError => this.Exception != null;
}

/// <summary>
/// 脚本文件操作事件参数.
/// </summary>
public class ScriptFileEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptFileEventArgs"/> class.
    /// </summary>
    /// <param name="action">执行的操作类型.</param>
    /// <param name="scriptName">当前的脚本名称.</param>
    /// <param name="filePath">当前的物理文件全路径.</param>
    /// <param name="oldFilePath">操作前的旧物理文件路径（仅在 Renamed 或 SavedAs 时有效）.</param>
    public ScriptFileEventArgs(ScriptFileAction action, string scriptName, string filePath, string? oldFilePath = null)
    {
        Action = action;
        ScriptName = scriptName;
        FilePath = filePath;
        OldFilePath = oldFilePath;
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Gets 执行的操作类型.
    /// </summary>
    public ScriptFileAction Action { get; }

    /// <summary>
    /// Gets 当前的脚本名称.
    /// </summary>
    public string ScriptName { get; }

    /// <summary>
    /// Gets 当前的物理文件全路径.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets 操作前的旧物理文件路径（仅在 Renamed 或 SavedAs 时有效）.
    /// </summary>
    public string? OldFilePath { get; }

    /// <summary>
    /// Gets 事件发生的时刻.
    /// </summary>
    public DateTime Timestamp { get; }
}