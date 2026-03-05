using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BgCommon.Script.Compiler.Security;

/// <summary>
/// 禁止文件操作的脚本安全策略.
/// </summary>
public class NoFileAccessSecurityPolicy : IScriptSecurityPolicy
{
    /// <summary>
    /// 校验语法树中是否包含文件操作相关代码.
    /// </summary>
    /// <param name="tree">待校验的语法树.</param>
    /// <param name="compilation">编译上下文.</param>
    /// <exception cref="InvalidOperationException">包含文件操作时抛出异常.</exception>
    public void Validate(SyntaxTree tree, CSharpCompilation compilation)
    {
        // 空值校验（符合接口契约）
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(compilation);

        // 获取语法树的根节点
        var root = tree.GetRoot();

        // 1. 语法层面：查找所有引用 System.IO 的 using 语句
        var usingDirectives = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name.ToString().StartsWith("System.IO"));

        if (usingDirectives.Any())
        {
            throw new InvalidOperationException("脚本禁止引用 System.IO 命名空间（文件操作）");
        }

        // 2. 语义层面：查找所有调用 File/FileStream 等类型的表达式
        var semanticModel = compilation.GetSemanticModel(tree);
        var memberAccessExpressions = root.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>();

        foreach (var expr in memberAccessExpressions)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expr);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                // 检查方法所属的类型是否为文件操作相关类型
                var containingType = methodSymbol.ContainingType;
                if (containingType != null &&
                    (containingType.Name == "File" ||
                     containingType.Name == "FileStream" ||
                     containingType.Name == "Directory"))
                {
                    throw new InvalidOperationException(
                        $"脚本禁止调用 {containingType.Name} 类的方法（文件操作）: {methodSymbol.Name}");
                }
            }
        }
    }
}