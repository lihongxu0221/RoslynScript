using BgCommon.Script.Runtime.DotNet;
using Microsoft.CodeAnalysis;

namespace BgCommon.Script.Runtime.Compilation;

/// <summary>
/// Gets pre-processor symbols supported in user script code.
/// </summary>
public static class PreprocessorSymbols
{
    private static readonly string[] DebugSymbols = ["BAIGU", "DEBUG", "TRACE"];
    private static readonly string[] ReleaseSymbols = ["BAIGU", "RELEASE"];

    public static string[] For(OptimizationLevel optimizationLevel) =>
        (optimizationLevel == OptimizationLevel.Debug ? DebugSymbols : ReleaseSymbols).ToArray();

    public static string[] For(DotNetFrameworkVersion version)
    {
        var major = version.GetMajorVersion();

        var symbols = new List<string>
        {
            "BAIGU",
            "NET",
            $"NET{major}_0",
        };

        // Add all past versions symbols
        while (major >= 5)
        {
            symbols.Add($"NET{major}_0_OR_GREATER");
            major--;
        }

        return symbols.ToArray();
    }

    public static string[] For(OptimizationLevel optimizationLevel, DotNetFrameworkVersion version) =>
        For(optimizationLevel).Union(For(version)).ToArray();
}