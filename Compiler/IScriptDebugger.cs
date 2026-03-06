using System;
using System.Threading.Tasks;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 定义脚本调试器的核心功能接口.
/// </summary>
public interface IScriptDebugger
{
    /// <summary>
    /// 命中断点时触发的事件.
    /// </summary>
    event EventHandler<DebugEventArgs> OnBreakpointHit;

    /// <summary>
    /// Gets or sets 调试器的当前步进模式.
    /// </summary>
    StepMode StepMode { get; set; }

    /// <summary>
    /// 将调试器附加到指定的脚本上下文.
    /// </summary>
    /// <param name="context">脚本执行上下文对象.</param>
    /// <returns>返回一个异步任务，结果表示是否成功附加.</returns>
    Task<bool> AttachDebuggerAsync(ScriptContext context);

    /// <summary>
    /// 在指定的脚本行设置断点.
    /// </summary>
    /// <param name="scriptName">脚本的名称或路径.</param>
    /// <param name="line">需要设置断点的行号.</param>
    void SetBreakpoint(string scriptName, int line);
}