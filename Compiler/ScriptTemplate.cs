namespace BgCommon.Script.Compiler;

/// <summary>
/// 表示脚本模板的配置信息类.
/// </summary>
public class ScriptTemplate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptTemplate"/> class.
    /// </summary>
    public ScriptTemplate()
    {
        this.ReferLibs = new List<string>();
        this.Extension = "tpl";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptTemplate"/> class.
    /// </summary>
    /// <param name="templateName">脚本模板的名称.</param>
    /// <param name="extension">脚本模板的扩展名.</param>
    /// <param name="templatePath">脚本模板文件的路径.</param>
    /// <param name="referLibsPath">依赖库所在的根路径.</param>
    /// <param name="referLibs">需要引用的程序集名称列表.</param>
    public ScriptTemplate(string templateName, string extension, string templatePath, string referLibsPath, params string[] referLibs)
    {
        // 验证参数是否为空.
        ArgumentNullException.ThrowIfNull(templateName, nameof(templateName));
        ArgumentNullException.ThrowIfNull(templatePath, nameof(templatePath));
        ArgumentNullException.ThrowIfNull(referLibsPath, nameof(referLibsPath));
        ArgumentNullException.ThrowIfNull(referLibs, nameof(referLibs));

        if (string.IsNullOrEmpty(extension))
        {
            extension = "tpl";
        }

        // 为属性赋值，显式使用 this 标识.
        this.TemplateName = templateName;
        this.Extension = extension;
        this.TemplatePath = templatePath;
        this.ReferLibsPath = referLibsPath;

        // 初始化引用库列表.
        this.ReferLibs = new List<string>(referLibs);
    }

    /// <summary>
    /// Gets or sets 模板文件扩展名.
    /// </summary>
    public string Extension { get; set; } = "tpl";

    /// <summary>
    /// Gets or sets 模板名称.
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets 模板文件所在的文件夹路径.
    /// </summary>
    public string TemplatePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets 该模板对应的代码源文件全路径（用于 LoadAsync 加载初始代码）.
    /// 假设模板代码文件与配置同名且为 .cs 后缀.
    /// </summary>
    public string CodeTemplateFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(this.TemplatePath) || string.IsNullOrEmpty(this.TemplateName))
            {
                return string.Empty;
            }

            return Path.Combine(this.TemplatePath, $"{this.TemplateName}.{this.Extension}");
        }
    }

    /// <summary>
    /// Gets or sets 引用程序集所在路径.
    /// </summary>
    public string ReferLibsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets 模板引用的程序集列表.
    /// </summary>
    /// <remarks>
    /// 例如： "System.Xml.dll" 或 "3th\BgCommon.dll" .
    /// </remarks>
    public List<string> ReferLibs { get; set; } = new List<string>();
}