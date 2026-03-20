namespace BgCommon.Script.Runtime.Models;

/// <summary>
/// 表示脚本的版本信息实体.
/// </summary>
[Serializable]
public class ScriptVersion : ObservableObject
{
    private string id = string.Empty;
    private string scriptName = string.Empty;
    private ScriptFile? script;
    private DateTime createdAt;
    private string author = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptVersion"/> class.
    /// </summary>
    public ScriptVersion()
    {
        // 默认初始化创建时间
        this.createdAt = DateTime.Now;
    }

    /// <summary>
    /// Gets or sets 版本唯一标识符.
    /// </summary>
    public string Id
    {
        get => this.id;
        set => this.SetProperty(ref this.id, value);
    }

    /// <summary>
    /// Gets or sets 脚本名称.
    /// </summary>
    public string ScriptName
    {
        get => this.scriptName;
        set => this.SetProperty(ref this.scriptName, value);
    }

    /// <summary>
    /// Gets or sets 脚本内容源码.
    /// </summary>
    public ScriptFile? Script
    {
        get => this.script;
        set => this.SetProperty(ref this.script, value);
    }

    /// <summary>
    /// Gets or sets 版本创建时间.
    /// </summary>
    public DateTime CreatedAt
    {
        get => this.createdAt;
        set => this.SetProperty(ref this.createdAt, value);
    }

    /// <summary>
    /// Gets or sets 版本创建者名称.
    /// </summary>
    public string Author
    {
        get => this.author;
        set => this.SetProperty(ref this.author, value);
    }
}