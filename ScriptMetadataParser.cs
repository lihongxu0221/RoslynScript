using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BgCommon.Script;

/// <summary>
/// 提供对脚本源代码进行静态分析的工具类.
/// </summary>
public static class ScriptMetadataParser
{
    // 用于匹配 XML 标签的简单正则（作为解析失败时的回退方案）
    private static readonly Regex XmlTagRegex = new Regex(@"<[^>]*>", RegexOptions.Compiled);
    private static readonly string[] SplitTags = new string[] { "\r\n", "\r", "\n" };

    /// <summary>
    /// 从脚本代码中提取顶部的说明文字.
    /// 优先解析 summary 标签，若无则提取第一段连续注释.
    /// </summary>
    /// <param name="code">脚本源码.</param>
    /// <returns>清洗后的摘要文本.</returns>
    public static string GetSummary(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        try
        {
            // 解析语法树
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = syntaxTree.GetCompilationUnitRoot();

            // 获取代码第一个有效标记（Token）之前的全部琐碎内容（Trivia）
            var leadingTrivia = root.GetLeadingTrivia();

            // 1. 优先寻找文档注释（即 /// 格式）
            var docComment = leadingTrivia .FirstOrDefault(t =>
                t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            if (docComment != default)
            {
                return CleanDocumentationComment(docComment.ToFullString());
            }

            // 2. 如果没有文档注释，则寻找普通的单行或多行注释
            var regularComment = leadingTrivia
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                     t.IsKind(SyntaxKind.MultiLineCommentTrivia));

            return regularComment != default
                ? CleanRegularComment(regularComment.ToString())
                : string.Empty;
        }
        catch
        {
            // 解析失败时返回空字符串，确保不干扰主程序运行
            return string.Empty;
        }

        // if (string.IsNullOrWhiteSpace(code))
        // {
        //     return string.Empty;
        // }
        //
        // var syntaxTree = CSharpSyntaxTree.ParseText(code);
        // var root = syntaxTree.GetCompilationUnitRoot();
        //
        // // 寻找第一个成员或整个文档的琐碎内容（Trivia）
        // var comment = root.GetLeadingTrivia()
        //     .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
        //                          t.IsKind(SyntaxKind.MultiLineCommentTrivia));
        //
        // return comment.ToString().Trim('/', '*', ' ', '\r', '\n');
    }

    /// <summary>
    /// 清理文档注释（处理 /// 和 XML 标签）.
    /// </summary>
    private static string CleanDocumentationComment(string rawComment)
    {
        // 移除每行开头的 /// 或 /**
        var lines = rawComment.Split(SplitTags, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim().TrimStart('/', '*').Trim());

        var content = string.Join(" ", lines);

        try
        {
            // 尝试通过 XML 解析提取 <summary> 内容
            // 注意：文档注释需要包装在根节点中才能解析
            var xmlContent = $"<root>{content}</root>";
            var xdoc = XDocument.Parse(xmlContent);
            var summary = xdoc.Descendants("summary").FirstOrDefault()?.Value;

            if (!string.IsNullOrWhiteSpace(summary))
            {
                return summary.Trim();
            }
        }
        catch
        {
            // XML 解析失败，说明不是标准格式，尝试正则移除所有标签
            content = XmlTagRegex.Replace(content, string.Empty);
        }

        return content.Trim();
    }

    /// <summary>
    /// 清理普通注释.
    /// </summary>
    private static string CleanRegularComment(string rawComment)
    {
        return rawComment
            .Trim('/', '*', ' ', '\r', '\n')
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }
}