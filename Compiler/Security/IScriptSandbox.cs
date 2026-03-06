using System;
using System.Threading.Tasks;
using BgCommon.Script.Compiler.Models;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 定义脚本沙箱环境的接口，用于安全地执行不可信脚本并限制其资源使用.
/// </summary>
public interface IScriptSandbox
{
    /// <summary>
    /// Gets a value indicating whether 沙箱当前是否正在运行脚本.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 初始化沙箱环境.
    /// </summary>
    /// <returns>返回异步初始化任务.</returns>
    Task InitializeAsync();

    /// <summary>
    /// 在沙箱中执行指定的脚本文件.
    /// </summary>
    /// <param name="script">要执行的脚本文件对象.</param>
    /// <param name="context">脚本执行上下文.</param>
    /// <returns>返回执行结果对象.</returns>
    Task<object?> ExecuteAsync(ScriptFile script, ScriptContext context);

    /// <summary>
    /// 设置脚本的权限集.
    /// </summary>
    /// <param name="permissions">脚本权限设置对象.</param>
    void SetPermissionSet(ScriptPermissionSet permissions);

    /// <summary>
    /// 将指定的程序集添加到白名单中.
    /// </summary>
    /// <param name="assemblyName">程序集的名称.</param>
    void WhitelistAssembly(string assemblyName);

    /// <summary>
    /// 将指定的类型添加到黑名单中，禁止脚本访问.
    /// </summary>
    /// <param name="typeName">类型的完整限定名称.</param>
    void BlacklistType(string typeName);

    /// <summary>
    /// 设置脚本执行时的内存限制.
    /// </summary>
    /// <param name="bytes">允许使用的最大内存字节数.</param>
    void SetMemoryLimit(long bytes);

    /// <summary>
    /// 设置脚本执行时的 CPU 使用率限制.
    /// </summary>
    /// <param name="percentage">允许使用的 CPU 百分比（0.0 到 1.0）.</param>
    void SetCpuLimit(double percentage);

    /// <summary>
    /// 强制停止当前正在执行的脚本并清理资源.
    /// </summary>
    void Terminate();
}