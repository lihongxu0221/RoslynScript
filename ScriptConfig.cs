namespace BgCommon.Script;

/// <summary>
/// 表示脚本的配置信息类，用于定义程序集路径、脚本路径及相关的引用关系.
/// </summary>
public class ScriptConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptConfig"/> class.
    /// </summary>
    public ScriptConfig()
    {
        this.Namespaces = new List<string>();
        this.ReferLibs = new List<string>();
        this.Templates = new List<ScriptTemplate>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptConfig"/> class.
    /// </summary>
    /// <param name="referLibsPath">引用程序集所在路径.</param>
    /// <param name="scriptPath">脚本文件存储路径.</param>
    /// <param name="templatePath">模板文件存储路径.</param>
    public ScriptConfig(string referLibsPath, string scriptPath, string templatePath)
    {
        // 验证构造函数参数是否为空.
        ArgumentNullException.ThrowIfNull(referLibsPath, nameof(referLibsPath));
        ArgumentNullException.ThrowIfNull(scriptPath, nameof(scriptPath));
        ArgumentNullException.ThrowIfNull(templatePath, nameof(templatePath));

        // 为属性赋值，显式使用 this 标识.
        this.ReferLibsPath = referLibsPath;
        this.ScriptPath = scriptPath;
        this.TemplatePath = templatePath;
    }

    /// <summary>
    /// Gets or sets 引用程序集所在路径.
    /// </summary>
    public string ReferLibsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets 脚本文件扩展名.
    /// </summary>
    public string ScriptFileExtension { get; set; } = "csx";

    /// <summary>
    /// Gets or sets 脚本文件存储路径.
    /// </summary>
    public string ScriptPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets 脚本模板文件扩展名.
    /// </summary>
    public string TemplateFileExtension { get; set; } = "tpl";

    /// <summary>
    /// Gets or sets 模板文件存储路径.
    /// </summary>
    public string TemplatePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets 脚本预定义的命名空间列表.
    /// </summary>
    public List<string> Namespaces { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets 脚本需要引用的外部程序集列表.
    /// </summary>
    public List<string> ReferLibs { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets 脚本模板配置集合.
    /// </summary>
    public List<ScriptTemplate> Templates { get; set; } = new List<ScriptTemplate>();
}