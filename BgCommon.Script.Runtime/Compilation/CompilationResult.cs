using System.Reflection;
using Microsoft.CodeAnalysis;

namespace BgCommon.Script.Runtime.Compilation;

/// <summary>
/// 表示脚本编译的结果信息类.
/// </summary>
[Serializable]
public class CompilationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationResult"/> class.
    /// </summary>
    /// <param name="success">编译是否成功.</param>
    /// <param name="assemblyName">程序集名称信息.</param>
    /// <param name="assemblyFileName">程序集文件名.</param>
    /// <param name="assemblyBytes">程序集二进制数据.</param>
    /// <param name="diagnostics">编译诊断信息集合.</param>
    public CompilationResult(
        bool success,
        AssemblyName assemblyName,
        string assemblyFileName,
        byte[] assemblyBytes,
        ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(assemblyName, nameof(assemblyName));
        ArgumentNullException.ThrowIfNull(assemblyFileName, nameof(assemblyFileName));
        ArgumentNullException.ThrowIfNull(assemblyBytes, nameof(assemblyBytes));

        // 初始化各个属性字段
        this.Success = success;
        this.AssemblyName = assemblyName;
        this.AssemblyFileName = assemblyFileName;
        this.AssemblyData = assemblyBytes;
        this.Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets a value indicating whether 脚本编译是否成功.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets 编译生成的程序集名称信息.
    /// </summary>
    public AssemblyName AssemblyName { get; }

    /// <summary>
    /// Gets 程序集文件的名称.
    /// </summary>
    public string AssemblyFileName { get; }

    /// <summary>
    /// Gets 程序集的二进制字节数据.
    /// </summary>
    public byte[] AssemblyData { get; }

    /// <summary>
    /// Gets 编译过程中的诊断信息集合.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets 运行脚本结果.
    /// </summary>
    public object? ReturnValue { get; private set; }

    /// <summary>
    /// 设置运行结果并返回当前实例.
    /// </summary>
    /// <param name="returnValue">返回值对象.</param>
    /// <returns>编译结果实例.</returns>
    public CompilationResult WithReturnValue(object? returnValue)
    {
        this.ReturnValue = returnValue;
        return this;
    }
}