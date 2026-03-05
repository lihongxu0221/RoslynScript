using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BgCommon.Script.Compiler.Security;

/// <summary>
/// 脚本安全策略接口，用户可以自定义规则.
/// </summary>
public interface IScriptSecurityPolicy
{
    /// <summary>
    /// 在指定的 C# 编译上下文中验证指定的语法树是否符合安全策略.
    /// </summary>
    /// <param name="tree">需要验证的语法树，不能为 null.</param>
    /// <param name="compilation">提供验证语义上下文的 C# 编译对象，不能为 null.</param>
    void Validate(SyntaxTree tree, CSharpCompilation compilation);
}