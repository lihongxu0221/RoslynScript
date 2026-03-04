using BgCommon.Helpers;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace BgCommon.Script.Models;

/// <summary>
/// 表示脚本文件的元数据类,包含脚本的基本信息和配置.
/// 注意: 文件路径、扩展名等信息由 ScriptConfig 和外部管理器决定,不在此类中存储.
/// </summary>
[Serializable]
public partial class ScriptFile : ScriptBase
{
    private const string EncryptionPrefix = "baigu";

    private string password = string.Empty;
    private bool isEnabled = true;
    private int executionCount;

    [NonSerialized]
    private string? contentBackupBeforeSerialization;

    /// <summary>
    /// 最后一次执行耗时(运行时统计,不序列化).
    /// </summary>
    [NonSerialized]
    private TimeSpan lastExecutionDuration;

    /// <summary>
    /// 最后一次执行结果(运行时统计,不序列化).
    /// </summary>
    [NonSerialized]
    private string? lastExecutionResult;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptFile"/> class.
    /// </summary>
    public ScriptFile()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptFile"/> class.
    /// </summary>
    /// <param name="name">脚本名称(不含扩展名).</param>
    public ScriptFile(string name)
        : base(name)
    {
    }

    /// <summary>
    /// Gets or sets 脚本加密密码.
    /// </summary>
    public string Password
    {
        get => this.password;
        set => this.SetProperty(ref this.password, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether 脚本是否启用.
    /// </summary>
    public bool IsEnabled
    {
        get => this.isEnabled;
        set => SetProperty(ref this.isEnabled, value);
    }

    /// <summary>
    /// Gets or sets 脚本执行次数(累计).
    /// </summary>
    public int ExecutionCount
    {
        get => this.executionCount;
        set => SetProperty(ref this.executionCount, value);
    }

    /// <summary>
    /// Gets or sets 最后一次执行耗时(运行时统计,不序列化).
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public TimeSpan LastExecutionDuration
    {
        get => this.lastExecutionDuration;
        set => SetProperty(ref this.lastExecutionDuration, value);
    }

    /// <summary>
    /// Gets or sets 最后一次执行结果(运行时统计,不序列化).
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public string? LastExecutionResult
    {
        get => this.lastExecutionResult;
        set => SetProperty(ref this.lastExecutionResult, value);
    }

    /// <summary>
    /// 增加执行次数.
    /// </summary>
    public void IncrementExecutionCount()
    {
        this.ExecutionCount++;
    }

    /// <summary>
    /// 更新执行统计信息.
    /// </summary>
    /// <param name="duration">执行耗时.</param>
    /// <param name="result">执行结果.</param>
    public void UpdateExecutionStats(TimeSpan duration, string? result)
    {
        this.LastExecutionDuration = duration;
        this.LastExecutionResult = result;
    }

    /// <summary>
    /// 创建脚本文件的深拷贝.
    /// </summary>
    /// <returns>脚本文件的副本.</returns>
    public ScriptFile Clone()
    {
        var clone = new ScriptFile();
        clone.PatchMetadata(this);
        return clone;
    }

    /// <inheritdoc/>
    public override void PatchMetadata(ScriptBase? source)
    {
        base.PatchMetadata(source);
        if (source is ScriptFile sourceFile)
        {
            this.IsEnabled = sourceFile.IsEnabled;
            this.Password = sourceFile.Password;
            this.ExecutionCount = sourceFile.ExecutionCount;
        }
    }

    /// <summary>
    /// 获取脚本的详细信息.
    /// </summary>
    /// <returns>详细信息字符串.</returns>
    public override string GetDetailInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== 脚本信息 ===");
        sb.AppendLine($"名称: {this.Name}");
        sb.AppendLine($"脚本内容: {this.Content}");
        sb.AppendLine($"脚本内容长度: {this.Content.Length} 字符");
        sb.AppendLine($"密码: {this.password}");
        sb.AppendLine($"类型名: {this.TargetType}");
        sb.AppendLine($"执行方法: {this.TargetMethod}");
        sb.AppendLine($"引用库列表: {string.Join(",", this.ReferencedAssemblies)}");
        sb.AppendLine($"命名空间列表: {string.Join(Environment.NewLine, this.ReferencedAssemblies)}");
        sb.AppendLine($"分类: {this.Category ?? "未分类"}");
        sb.AppendLine($"摘要: {this.Summary ?? "无"}");
        sb.AppendLine($"描述: {this.Description ?? "无"}");
        sb.AppendLine($"作者: {this.Author ?? "未知"}");
        sb.AppendLine($"版本: {this.Version ?? "未指定"}");
        sb.AppendLine($"创建时间: {this.CreatedTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"修改时间: {this.ModifiedTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"标签: {string.Join(", ", this.Tags)}");
        sb.AppendLine($"输入参数: {this.Inputs.Count} 个");
        sb.AppendLine($"输出参数: {this.Outputs.Count} 个");
        sb.AppendLine($"启用状态: {(this.isEnabled ? "启用" : "禁用")}");
        sb.AppendLine($"执行次数: {this.executionCount}");
        sb.AppendLine($"最后执行耗时: {this.lastExecutionDuration.TotalMilliseconds:F2} ms");
        return sb.ToString();
    }

    /// <summary>
    /// 序列化前执行：对 SourceCode 进行加密处理.
    /// </summary>
    [OnSerializing]
    private void OnSerializing(StreamingContext context)
    {
        if (string.IsNullOrEmpty(this.Content) || string.IsNullOrEmpty(this.Password))
        {
            return; // 无内容或无密码，无需加密
        }

        // 备份明文到非序列化字段
        this.contentBackupBeforeSerialization = this.Content;

        try
        {
            // 1. SourceCode 和 Password 按位与操作
            string sourceCodeString = this.Content.Parse(this.Password);

            // 2. 拼接 Password + "\r\n" + SourceCodeString
            string contentToEncrypt = $"{this.password}\r\n{sourceCodeString}";

            // 3. 加密
            string encryptedContent = contentToEncrypt.Encrypt(EncryptionPrefix);

            // 4. 替换 SourceCode 为加密后的值（序列化时保存这个加密值）
            this.Content = encryptedContent;
        }
        catch (Exception ex)
        {
            // 恢复原始内容，避免序列化失败导致数据丢失
            if (this.Content != this.contentBackupBeforeSerialization)
            {
                this.Content = this.contentBackupBeforeSerialization;
                this.contentBackupBeforeSerialization = null;
            }

            throw new SerializationException("序列化时加密 SourceCode 失败", ex);
        }
    }

    /// <summary>
    /// 序列化完成后立即触发.
    /// </summary>
    /// <param name="context">二进制序列化上下文.</param>
    [OnSerialized]
    private void OnSerialized(StreamingContext context)
    {
        // 恢复内存中的明文代码
        if (this.contentBackupBeforeSerialization != null)
        {
            this.Content = this.contentBackupBeforeSerialization;
            this.contentBackupBeforeSerialization = null;
        }
    }

    /// <summary>
    /// 反序列化后执行：还原 SourceCode 的原始值.
    /// </summary>
    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (string.IsNullOrEmpty(this.Content) || string.IsNullOrEmpty(this.Password))
        {
            return; // 无加密内容或无密码，无需解密
        }

        try
        {
            // 1. 解密
            string decryptedContent = this.Content.Decrypt(EncryptionPrefix);

            // 2. 拆分 Password 和 SourceCodeString（按 \r\n 分割）
            string[] parts = decryptedContent.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (parts.Length != 2 || parts[0] != Password)
            {
                throw new InvalidDataException("解密后密码不匹配，无法还原 SourceCode");
            }

            string sourceCodeString = parts[1];

            // 3. 按位与的反向操作还原原始 SourceCode
            string originalSourceCode = sourceCodeString.Parse(Password);

            // 4. 写回原始 SourceCode
            this.Content = originalSourceCode;
        }
        catch (Exception ex)
        {
            throw new SerializationException("反序列化时解密 SourceCode 失败", ex);
        }
    }
}