using System;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 表示脚本执行的权限配置集合.
/// </summary>
[Serializable]
public class ScriptPermissionSet: ObservableObject
{
    private bool allowFileIO;
    private bool allowNetwork;
    private bool allowReflection;
    private bool allowSystemInformation;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptPermissionSet"/> class.
    /// </summary>
    public ScriptPermissionSet()
    {
        // 默认初始化为最严格权限
        this.allowFileIO = false;
        this.allowNetwork = false;
        this.allowReflection = false;
        this.allowSystemInformation = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether 是否允许文件输入输出操作.
    /// </summary>
    public bool AllowFileIO
    {
        get => this.allowFileIO;
        set => this.SetProperty(ref this.allowFileIO, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether 是否允许网络访问.
    /// </summary>
    public bool AllowNetwork
    {
        get => this.allowNetwork;
        set => this.SetProperty(ref this.allowNetwork, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether 是否允许使用反射功能.
    /// </summary>
    public bool AllowReflection
    {
        get => this.allowReflection;
        set => this.SetProperty(ref this.allowReflection, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether 是否允许获取系统信息.
    /// </summary>
    public bool AllowSystemInformation
    {
        get => this.allowSystemInformation;
        set => this.SetProperty(ref this.allowSystemInformation, value);
    }
}