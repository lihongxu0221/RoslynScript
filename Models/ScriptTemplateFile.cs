using System.ComponentModel;
using System.Text;

namespace BgCommon.Script.Models;

/// <summary>
/// 表示脚本模板文件的元数据类,包含模板的基本信息和配置.
/// 注意: 文件路径、扩展名等信息由 ScriptConfig 和外部管理器决定,不在此类中存储.
/// .tpl 文件是 ScriptTemplateFile 序列化后的默认扩展名,包含模板内容和元数据.
/// </summary>
[Serializable]
public partial class ScriptTemplateFile : ScriptBase
{
    private bool isBuiltin;
    private int usageCount;
    private string? icon;
    private string previewImage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptTemplateFile"/> class.
    /// </summary>
    public ScriptTemplateFile()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptTemplateFile"/> class.
    /// </summary>
    /// <param name="name">模板名称(不含扩展名).</param>
    public ScriptTemplateFile(string name)
        : base(name)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether 是否为内置模板.
    /// </summary>
    public bool IsBuiltin
    {
        get => this.isBuiltin;
        set => SetProperty(ref this.isBuiltin, value);
    }

    /// <summary>
    /// Gets or sets 模板使用次数.
    /// </summary>
    public int UsageCount
    {
        get => this.usageCount;
        set => SetProperty(ref this.usageCount, value);
    }

    /// <summary>
    /// Gets or sets 模板图标(可以是图标名称、Emoji 或图标路径).
    /// </summary>
    public string? Icon
    {
        get => this.icon;
        set => SetProperty(ref this.icon, value);
    }

    /// <summary>
    /// Gets or sets 模板预览图片路径.
    /// </summary>
    public string PreviewImage
    {
        get => this.previewImage;
        set => SetProperty(ref this.previewImage, value);
    }

    /// <summary>
    /// 增加使用次数.
    /// </summary>
    public void IncrementUsageCount()
    {
        this.UsageCount++;
    }

    /// <summary>
    /// 从模板创建新的脚本文件信息.
    /// </summary>
    /// <param name="newScriptName">新脚本名称.</param>
    /// <returns>新的脚本文件信息.</returns>
    public ScriptFile CreateScriptFile(string newScriptName)
    {
        var scriptFile = new ScriptFile(newScriptName)
        {
            Summary = this.Summary,
            Description = this.Description,
            Author = this.Author,
            Version = this.Version,
            Tags = new List<string>(this.Tags),
            ReferencedAssemblies = new List<string>(this.ReferencedAssemblies),
            Namespaces = new List<string>(this.Namespaces),
        };

        // 复制输入参数
        foreach (var param in this.Inputs)
        {
            scriptFile.AddInputParameter(param.Clone());
        }

        // 复制输出参数
        foreach (var param in this.Outputs)
        {
            scriptFile.AddOutputParameter(param.Clone());
        }

        // 更新使用统计
        this.IncrementUsageCount();

        return scriptFile;
    }

    /// <summary>
    /// 创建模板的深拷贝.
    /// </summary>
    /// <returns>模板的副本.</returns>
    public ScriptTemplateFile Clone()
    {
        var clone = new ScriptTemplateFile()
        {
            Id = Guid.NewGuid(),
            Name = this.Name,
            Content = this.Content,
            Summary = this.Summary,
            Description = this.Description,
            Author = this.Author,
            CreatedTime = this.CreatedTime,
            ModifiedTime = this.ModifiedTime,
            Version = this.Version,
            Category = this.Category,
            IsBuiltin = this.IsBuiltin,
            UsageCount = this.UsageCount,
            Icon = this.Icon,
            PreviewImage = this.PreviewImage,
            ReferencedAssemblies = new List<string>(this.ReferencedAssemblies),
            Namespaces = new List<string>(this.Namespaces),
        };

        clone.Inputs = this.Inputs.Select(p => p.Clone()).ToList();
        clone.Outputs = this.Outputs.Select(p => p.Clone()).ToList();
        clone.Tags = new List<string>(this.Tags);

        return clone;
    }

    /// <summary>
    /// 返回模板的字符串表示.
    /// </summary>
    /// <returns>模板信息.</returns>
    public override string ToString()
    {
        var iconStr = string.IsNullOrEmpty(this.icon) ? string.Empty : $"{this.icon} ";
        return $"{iconStr}{base.ToString()}";
    }

    /// <summary>
    /// 获取模板的详细信息.
    /// </summary>
    /// <returns>详细信息字符串.</returns>
    public override string GetDetailInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== 模板信息 ===");
        sb.AppendLine($"名称: {this.Name}");
        sb.AppendLine($"模板内容: {this.Content}");
        sb.AppendLine($"模板内容长度: {this.Content.Length} 字符");
        sb.AppendLine($"引用库列表: {string.Join(",", this.ReferencedAssemblies)}");
        sb.AppendLine($"命名空间列表: {string.Join(Environment.NewLine, this.ReferencedAssemblies)}");
        sb.AppendLine($"分类: {this.Category ?? "未分类"}");
        sb.AppendLine($"摘要: {this.Summary ?? "无"}");
        sb.AppendLine($"描述: {this.Description ?? "无"}");
        sb.AppendLine($"作者: {this.Author ?? "未知"}");
        sb.AppendLine($"版本: {this.Version ?? "未指定"}");
        sb.AppendLine($"图标: {this.Icon ?? "无"}");
        sb.AppendLine($"创建时间: {this.CreatedTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"修改时间: {this.ModifiedTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"使用次数: {this.UsageCount}");
        sb.AppendLine($"类型: {(this.IsBuiltin ? "内置模板" : "自定义模板")}");
        sb.AppendLine($"标签: {string.Join(", ", this.Tags)}");
        sb.AppendLine($"输入参数: {this.Inputs.Count} 个");
        sb.AppendLine($"输出参数: {this.Outputs.Count} 个");
        sb.AppendLine($"预览图: {this.previewImage ?? "无"}");

        return sb.ToString();
    }
}