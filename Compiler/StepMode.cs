namespace BgCommon.Script.Compiler;

/// <summary>
/// 表示调试器的步进模式.
/// </summary>
public enum StepMode
{
    /// <summary>
    /// 无步进模式.
    /// </summary>
    None,

    /// <summary>
    /// 步入：进入函数内部.
    /// </summary>
    Into,

    /// <summary>
    /// 步过：执行下一行，不进入函数内部.
    /// </summary>
    Over,

    /// <summary>
    /// 步出：执行完当前函数并跳出.
    /// </summary>
    Out,
}