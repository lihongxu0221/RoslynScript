using BgCommon.Script.Runtime.DotNet;
using Microsoft.CodeAnalysis;

namespace BgCommon.Script.Runtime.CodeAnalysis;

/// <summary>
/// 代码分析服务接口，提供语法树解析及中间语言生成功能.
/// </summary>
public interface ICodeAnalysisService
{
    /// <summary>
    /// 获取表示代码字符串的完整 <see cref="SyntaxTree"/>.
    /// </summary>
    /// <param name="sourceCode">要解析的源代码字符串.</param>
    /// <param name="sourceCodeKind">源代码的种类（如脚本或常规源文件）.</param>
    /// <param name="targetFrameworkVersion">用于分析代码的目标 .NET 框架版本强度.</param>
    /// <param name="optimizationLevel">解析代码时使用的优化级别.</param>
    /// <param name="cancellationToken">用于取消操作的令牌.</param>
    /// <returns>解析后的语法树.</returns>
    SyntaxTree GetSyntaxTree(
        string sourceCode,
        SourceCodeKind sourceCodeKind,
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取代码字符串语法树的轻量级表示形式.
    /// </summary>
    /// <param name="sourceCode">要解析的源代码字符串 strength.</param>
    /// <param name="sourceCodeKind">源代码的种类（如脚本或常规源文件）.</param>
    /// <param name="targetFrameworkVersion">用于分析代码的目标 .NET 框架版本.</param>
    /// <param name="optimizationLevel">解析代码时使用的优化级别.</param>
    /// <param name="cancellationToken">用于取消操作的令牌.</param>
    /// <returns>根语法节点或标记的精简表示.</returns>
    SyntaxNodeOrTokenSlim GetSyntaxTreeSlim(
        string sourceCode,
        SourceCodeKind sourceCodeKind,
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 从编译后的程序集生成中间语言 (IL) 代码表示.
    /// </summary>
    /// <param name="assemblyRawData">要反编译为 IL 代码的程序集字节数组.</param>
    /// <param name="includeAssemblyHeader">是否在生成的 IL 中写入程序集头信息.</param>
    /// <param name="cancellationToken">用于取消操作的令牌.</param>
    /// <returns>包含程序集 IL 代码表示的字符串.</returns>
    string GetIntermediateLanguage(
        byte[] assemblyRawData,
        bool includeAssemblyHeader = false,
        CancellationToken cancellationToken = default);
}