namespace BgCommon.Script.Compiler.Models;

/// <summary>
/// 脚本配置基类.
/// </summary>
[Serializable]
public abstract partial class ScriptBase : ObservableObject
{
    private Guid id = Guid.NewGuid();
    private string name = string.Empty;
    private string content = string.Empty;
    private List<string> referencedAssemblies = new();
    private List<string> namespaces = new();
    private string targetType = string.Empty;
    private string targetMethod = string.Empty;
    private string summary = string.Empty;
    private string description = string.Empty;
    private string author = string.Empty;
    private DateTime createdTime = DateTime.Now;
    private DateTime modifiedTime = DateTime.Now;
    private string category = string.Empty;
    private string? version;
    private List<InputParam> inputs = new();
    private List<OutputParam> outputs = new();
    private List<string> tags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptBase"/> class.
    /// </summary>
    public ScriptBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptBase"/> class.
    /// </summary>
    /// <param name="name">模板名称(不含扩展名).</param>
    public ScriptBase(string name)
    {
        this.Name = name;
    }

    /// <summary>
    /// Gets or sets 唯一标识符.
    /// </summary>
    public Guid Id
    {
        get => this.id;
        set => this.SetProperty(ref this.id, value);
    }

    /// <summary>
    /// Gets or sets 脚本名称(不含扩展名).
    /// </summary>
    public string Name
    {
        get => this.name;
        set => SetProperty(ref this.name, value);
    }

    /// <summary>
    /// Gets or sets 脚本内容.
    /// </summary>
    public string Content
    {
        get => this.content;
        set => this.SetProperty(ref this.content, value);
    }

    /// <summary>
    /// Gets or sets 引用程序集列表.
    /// </summary>
    public List<string> ReferencedAssemblies
    {
        get => this.referencedAssemblies;
        set => this.SetProperty(ref this.referencedAssemblies, value);
    }

    /// <summary>
    /// Gets or sets 导入的命名空间列表.
    /// </summary>
    public List<string> Namespaces
    {
        get => this.namespaces;
        set => this.SetProperty(ref this.namespaces, value);
    }

    /// <summary>
    /// Gets or sets 与此实例关联的目标对象的类型名称.
    /// </summary>
    public string TargetType
    {
        get => this.targetType;
        set => this.SetProperty(ref this.targetType, value);
    }

    /// <summary>
    /// Gets or sets 操作目标的方法名称.
    /// </summary>
    public string TargetMethod
    {
        get => this.targetMethod;
        set => this.SetProperty(ref this.targetMethod, value);
    }

    /// <summary>
    /// Gets or sets 模板摘要(从代码注释中提取).
    /// </summary>
    public string Summary
    {
        get => this.summary;
        set => SetProperty(ref this.summary, value);
    }

    /// <summary>
    /// Gets or sets 脚本详细描述.
    /// </summary>
    public string Description
    {
        get => this.description;
        set => SetProperty(ref this.description, value);
    }

    /// <summary>
    /// Gets or sets 脚本作者.
    /// </summary>
    public string Author
    {
        get => this.author;
        set => SetProperty(ref this.author, value);
    }

    /// <summary>
    /// Gets or sets 脚本创建时间.
    /// </summary>
    public DateTime CreatedTime
    {
        get => this.createdTime;
        set => SetProperty(ref this.createdTime, value);
    }

    /// <summary>
    /// Gets or sets 脚本最后修改时间.
    /// </summary>
    public DateTime ModifiedTime
    {
        get => this.modifiedTime;
        set => SetProperty(ref this.modifiedTime, value);
    }

    /// <summary>
    /// Gets or sets 脚本版本号.
    /// </summary>
    public string? Version
    {
        get => this.version;
        set => SetProperty(ref this.version, value);
    }

    /// <summary>
    /// Gets or sets 分类(如:数据处理、文件操作、网络请求等).
    /// </summary>
    public string Category
    {
        get => this.category;
        set => SetProperty(ref this.category, value);
    }

    /// <summary>
    /// Gets or sets 输入参数列表.
    /// </summary>
    public List<InputParam> Inputs
    {
        get => this.inputs;
        set => SetProperty(ref this.inputs, value);
    }

    /// <summary>
    /// Gets or sets 输出参数列表.
    /// </summary>
    public List<OutputParam> Outputs
    {
        get => this.outputs;
        set => SetProperty(ref this.outputs, value);
    }

    /// <summary>
    /// Gets or sets 模板标签列表.
    /// </summary>
    public List<string> Tags
    {
        get => this.tags;
        set => SetProperty(ref this.tags, value);
    }

    /// <summary>
    /// 添加输入参数.
    /// </summary>
    /// <param name="parameter">输入参数.</param>
    public void AddInputParameter(InputParam parameter)
    {
        this.inputs.Add(parameter);
        OnPropertyChanged(nameof(this.Inputs));
    }

    /// <summary>
    /// 移除输入参数.
    /// </summary>
    /// <param name="parameter">输入参数.</param>
    public void RemoveInputParameter(InputParam parameter)
    {
        this.inputs.Remove(parameter);
        OnPropertyChanged(nameof(this.Inputs));
    }

    /// <summary>
    /// 添加输出参数.
    /// </summary>
    /// <param name="parameter">输出参数.</param>
    public void AddOutputParameter(OutputParam parameter)
    {
        this.outputs.Add(parameter);
        OnPropertyChanged(nameof(this.Outputs));
    }

    /// <summary>
    /// 移除输出参数.
    /// </summary>
    /// <param name="parameter">输出参数.</param>
    public void RemoveOutputParameter(OutputParam parameter)
    {
        this.outputs.Remove(parameter);
        OnPropertyChanged(nameof(this.Outputs));
    }

    /// <summary>
    /// 添加标签.
    /// </summary>
    /// <param name="tag">标签.</param>
    public void AddTag(string tag)
    {
        if (!string.IsNullOrEmpty(tag) && !this.tags.Contains(tag))
        {
            this.tags.Add(tag);
            OnPropertyChanged(nameof(this.Tags));
        }
    }

    /// <summary>
    /// 移除标签.
    /// </summary>
    /// <param name="tag">标签.</param>
    public void RemoveTag(string tag)
    {
        this.tags.Remove(tag);
        OnPropertyChanged(nameof(this.Tags));
    }

    /// <summary>
    /// 检查是否包含指定标签.
    /// </summary>
    /// <param name="tag">标签.</param>
    /// <returns>是否包含.</returns>
    public bool HasTag(string tag)
    {
        return this.tags.Contains(tag);
    }

    /// <summary>
    /// 将源实体（元数据属性值）拷贝到当前实体中.
    /// </summary>
    /// <param name="source">源实体.</param>
    public virtual void PatchMetadata(ScriptBase? source)
    {
        if (source == null)
        {
            return;
        }

        // --- 核心内容同步 ---
        // 注意：不拷贝 Id 以保持内存/克隆对象的同一性隔离
        this.Name = source.Name;
        this.Content = source.Content;

        // --- 其他元数据同步 ---
        this.ReferencedAssemblies.Clear();
        this.ReferencedAssemblies.AddRange(source.ReferencedAssemblies);
        this.Namespaces.Clear();
        this.Namespaces.AddRange(source.Namespaces);
        this.TargetType = source.TargetType;
        this.TargetMethod = source.TargetMethod;
        this.Summary = source.Summary;
        this.Description = source.Description;
        this.Author = source.Author;
        this.Category = source.Category;
        this.Version = source.Version;
        this.ModifiedTime = source.ModifiedTime;
        this.CreatedTime = source.CreatedTime;
        this.Inputs.Clear();
        this.Inputs.AddRange(source.Inputs.Select(p => p.Clone()));
        this.Outputs.Clear();
        this.Outputs.AddRange(source.Outputs.Select(p => p.Clone()));
        this.Tags.Clear();
        this.Tags.AddRange(source.Tags);

        this.OnPropertyChanged(nameof(ReferencedAssemblies));
        this.OnPropertyChanged(nameof(Namespaces));
        this.OnPropertyChanged(nameof(Inputs));
        this.OnPropertyChanged(nameof(Outputs));
        this.OnPropertyChanged(nameof(Tags));
    }

    /// <summary>
    /// 获取模板的详细信息.
    /// </summary>
    /// <returns>详细信息字符串.</returns>
    public virtual string GetDetailInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== 信息 ===");
        sb.AppendLine($"名称: {this.name}");
        sb.AppendLine($"内容: {this.content}");
        sb.AppendLine($"内容长度: {this.content?.Length ?? 0} 字符");
        sb.AppendLine($"类型名: {this.TargetType}");
        sb.AppendLine($"执行方法: {this.TargetMethod}");
        sb.AppendLine($"引用库列表: {string.Join(",", this.ReferencedAssemblies)}");
        sb.AppendLine($"命名空间列表: {string.Join(Environment.NewLine, this.ReferencedAssemblies)}");
        sb.AppendLine($"分类: {this.category ?? "未分类"}");
        sb.AppendLine($"摘要: {this.summary ?? "无"}");
        sb.AppendLine($"描述: {this.description ?? "无"}");
        sb.AppendLine($"作者: {this.author ?? "未知"}");
        sb.AppendLine($"版本: {this.version ?? "未指定"}");
        sb.AppendLine($"创建时间: {this.createdTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"修改时间: {this.modifiedTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"标签: {string.Join(", ", this.tags)}");
        sb.AppendLine($"输入参数: {this.inputs.Count} 个");
        sb.AppendLine($"输出参数: {this.outputs.Count} 个");
        return sb.ToString();
    }

    /// <summary>
    /// 返回脚本的字符串表示.
    /// </summary>
    /// <returns>脚本信息.</returns>
    public override string ToString()
    {
        return $"{this.Name} - {this.Summary ?? "无描述"}";
    }
}