using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace BgCommon.Script.Runtime.CodeAnalysis;

public class SyntaxTriviaSlim(SyntaxKind kind, LinePositionSpan span, string? displayValue = null)
{
    public SyntaxKind Kind { get; } = kind;

    public LinePositionSpan Span { get; } = span;

    public string? DisplayValue { get; } = displayValue.Truncate(50, true);
}