namespace BgCommon.Script.Compiler;

/// <summary>
/// 表示脚本调试事件的参数类.
/// </summary>
public class DebugEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebugEventArgs"/> class.
    /// </summary>
    /// <param name="scriptName">命中断点的脚本名称.</param>
    /// <param name="lineNumber">命中断点的行号.</param>
    public DebugEventArgs(string scriptName, int lineNumber)
    {
        // 验证脚本名称不能为空
        ArgumentNullException.ThrowIfNull(scriptName, nameof(scriptName));
        this.ScriptName = scriptName;
        this.LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets 脚本名称.
    /// </summary>
    public string ScriptName { get; }

    /// <summary>
    /// Gets 命中断点的行号.
    /// </summary>
    public int LineNumber { get; }
}