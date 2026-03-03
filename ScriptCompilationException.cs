using Microsoft.CodeAnalysis;

namespace BgCommon.Script;

/// <summary>
/// 表示脚本编译错误的异常类.
/// </summary>
public class ScriptCompilationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompilationException"/> class.
    /// </summary>
    /// <param name="diagnostics">诊断信息.</param>
    public ScriptCompilationException(IEnumerable<Diagnostic> diagnostics)
        : base("脚本编译失败，请检查诊断信息。")
    {
        this.Diagnostics = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    }

    /// <summary>
    /// Gets 详细的编译诊断信息.
    /// </summary>
    public List<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets 详细的诊断信息结果字符串.
    /// </summary>
    public override string Message
    {
        get
        {
            var errors = Diagnostics.Select(d => $"[{d.Id}] {d.GetMessage()} at {d.Location.GetLineSpan().StartLinePosition}");
            return base.Message + Environment.NewLine + string.Join(Environment.NewLine, errors);
        }
    }
}