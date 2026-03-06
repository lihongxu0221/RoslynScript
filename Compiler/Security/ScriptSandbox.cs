using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BgCommon.Script.Compiler.Models;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 脚本沙箱接口的默认实现类，增加了取消令牌支持.
/// </summary>
public class ScriptSandbox : IScriptSandbox
{
    private readonly HashSet<string> whitelistedAssemblies;
    private readonly HashSet<string> blacklistedTypes;
    private CancellationTokenSource? cancellationSource;
    private ScriptPermissionSet? permissionSet;
    private long memoryLimitBytes;
    private double cpuLimitPercentage;
    private volatile bool isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptSandbox"/> class.
    /// </summary>
    public ScriptSandbox()
    {
        this.whitelistedAssemblies = new HashSet<string>();
        this.blacklistedTypes = new HashSet<string>();
        this.isRunning = false;
    }

    /// <summary>
    /// Gets a value indicating whether 沙箱当前是否正在运行脚本.
    /// </summary>
    public bool IsRunning
    {
        get => this.isRunning;
    }

    /// <summary>
    /// 异步初始化沙箱环境.
    /// </summary>
    /// <returns>返回异步任务.</returns>
    public virtual async Task InitializeAsync()
    {
        // 初始化资源
        await Task.CompletedTask;
    }

    /// <summary>
    /// 在沙箱中安全地执行脚本.
    /// </summary>
    /// <param name="scriptFile">要执行的脚本文件.</param>
    /// <param name="scriptContext">执行上下文.</param>
    /// <returns>执行后的返回结果.</returns>
    public virtual async Task<object?> ExecuteAsync(ScriptFile scriptFile, ScriptContext scriptContext)
    {
        ArgumentNullException.ThrowIfNull(scriptFile, nameof(scriptFile));
        ArgumentNullException.ThrowIfNull(scriptContext, nameof(scriptContext));

        // 检查是否重复运行
        if (this.isRunning)
        {
            throw new InvalidOperationException("沙箱当前正在运行其他脚本.");
        }

        this.isRunning = true;
        this.cancellationSource = new CancellationTokenSource();

        try
        {
            // 传递取消令牌，支持在异步操作中响应 Terminate
            CancellationToken token = this.cancellationSource.Token;
            return await Task.Run(() => this.InternalExecute(scriptFile, scriptContext, token), token).ConfigureAwait(false);
        }
        finally
        {
            this.isRunning = false;
            this.cancellationSource?.Dispose();
            this.cancellationSource = null;
        }
    }

    /// <summary>
    /// 将程序集添加到白名单.
    /// </summary>
    /// <param name="assemblyName">程序集名称.</param>
    public void WhitelistAssembly(string assemblyName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName, nameof(assemblyName));
        this.whitelistedAssemblies.Add(assemblyName);
    }

    /// <summary>
    /// 将类型添加到黑名单.
    /// </summary>
    /// <param name="typeName">类型限定名.</param>
    public void BlacklistType(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName, nameof(typeName));
        this.blacklistedTypes.Add(typeName);
    }

    /// <summary>
    /// 设置权限集.
    /// </summary>
    /// <param name="permissions">权限对象.</param>
    public void SetPermissionSet(ScriptPermissionSet permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions, nameof(permissions));
        this.permissionSet = permissions;
    }

    /// <summary>
    /// 设置内存限制.
    /// </summary>
    /// <param name="bytes">字节数.</param>
    public void SetMemoryLimit(long bytes)
    {
        this.memoryLimitBytes = bytes;
    }

    /// <summary>
    /// 设置 CPU 限制.
    /// </summary>
    /// <param name="percentage">百分比.</param>
    public void SetCpuLimit(double percentage)
    {
        this.cpuLimitPercentage = Math.Clamp(percentage, 0.0, 1.0);
    }

    /// <summary>
    /// 强制终止当前执行的任务.
    /// </summary>
    public void Terminate()
    {
        // 触发取消信号
        this.cancellationSource?.Cancel();
        this.isRunning = false;
    }

    /// <summary>
    /// 内部执行逻辑.
    /// </summary>
    /// <param name="file">脚本文件.</param>
    /// <param name="context">上下文.</param>
    /// <param name="token">取消令牌.</param>
    /// <returns>执行结果.</returns>
    private object? InternalExecute(ScriptFile file, ScriptContext context, CancellationToken token)
    {
        // 模拟周期性检查令牌
        token.ThrowIfCancellationRequested();
        return null;
    }
}