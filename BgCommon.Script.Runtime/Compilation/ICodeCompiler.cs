namespace BgCommon.Script.Runtime.Compilation;

/// <summary>
/// 代码编译器接口，用于处理用户脚本代码的编译逻辑.
/// </summary>
public interface ICodeCompiler
{
    /// <summary>
    /// 编译用户编写的脚本代码.
    /// </summary>
    /// <param name="input">编译输入信息，包含源代码、引用项及编译选项.</param>
    /// <returns>编译结果对象，包含程序集信息或编译错误列表.</returns>
    CompilationResult Compile(CompilationInput input);

    /// <summary>
    /// 编译并运行用户编写的脚本代码.
    /// </summary>
    /// <param name="input">编译输入信息.</param>
    /// <param name="recompile">是否强制重新编译. 如果为 false 且缓存中存在，则使用旧程序集.</param>
    /// <param name="unloadAfterExecution">执行完成后是否立即卸载上下文并清理内存.</param>
    /// <returns>编译结果对象.</returns>
    Task<CompilationResult> RunAsync(
        CompilationInput input,
        bool recompile = true,
        bool unloadAfterExecution = false);

    /// <summary>
    /// 从上下文中销毁指定脚本编译结果.
    /// </summary>
    /// <param name="input">编译输入信息.</param>
    void Destory(CompilationInput input);
}