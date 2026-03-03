using System.ComponentModel;
using System.Text;

namespace BgCommon.Script.Models;

/// <summary>
/// 表示脚本输出参数的类,用于存储脚本执行后的输出结果.
/// </summary>
[Serializable]
public partial class OutputParam : ObservableObject
{
    private string name = string.Empty;
    private Type valueType = typeof(string);
    private object? value;
    private string description = string.Empty;
    private bool isReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParam"/> class.
    /// </summary>
    public OutputParam()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParam"/> class.
    /// </summary>
    /// <param name="name">参数名称.</param>
    /// <param name="value">参数值.</param>
    public OutputParam(string name, object? value = null)
    {
        this.name = name;
        this.value = value;
        this.valueType = value?.GetType() ?? typeof(object);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParam"/> class.
    /// </summary>
    /// <param name="name">参数名称.</param>
    /// <param name="valueType">参数值类型.</param>
    /// <param name="value">参数值.</param>
    public OutputParam(string name, Type valueType, object? value = null)
    {
        this.name = name;
        this.valueType = valueType;
        this.value = value;
    }

    /// <summary>
    /// Gets or sets 参数名称.
    /// </summary>
    public string Name
    {
        get => this.name;
        set => SetProperty(ref this.name, value);
    }

    /// <summary>
    /// Gets or sets 参数值类型.
    /// </summary>
    public Type ValueType
    {
        get => this.valueType;
        set => SetProperty(ref this.valueType, value);
    }

    /// <summary>
    /// Gets or sets 参数值.
    /// </summary>
    public object? Value
    {
        get => this.value;
        set => SetProperty(ref this.value, value);
    }

    /// <summary>
    /// Gets or sets 参数描述信息.
    /// </summary>
    public string Description
    {
        get => this.description;
        set => SetProperty(ref this.description, value);
    }

    /// <summary>
    /// Gets or sets 是否为只读参数.
    /// </summary>
    public bool IsReadOnly
    {
        get => this.isReadOnly;
        set => SetProperty(ref this.isReadOnly, value);
    }

    /// <summary>
    /// 获取参数的显示名称(名称 + 描述).
    /// </summary>
    /// <returns>显示名称.</returns>
    public string GetDisplayName()
    {
        if (string.IsNullOrEmpty(this.description))
        {
            return this.name;
        }

        return $"{this.name} ({this.description})";
    }

    /// <summary>
    /// 尝试将值转换为指定类型.
    /// </summary>
    /// <typeparam name="T">目标类型.</typeparam>
    /// <returns>转换后的值,失败返回默认值.</returns>
    public T? GetValueAs<T>()
    {
        if (this.value == null)
        {
            return default;
        }

        try
        {
            return (T?)Convert.ChangeType(this.value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 尝试获取值,失败时返回指定的默认值.
    /// </summary>
    /// <typeparam name="T">目标类型.</typeparam>
    /// <param name="defaultValue">默认值.</param>
    /// <returns>转换后的值.</returns>
    public T GetValueOrDefault<T>(T defaultValue)
    {
        var result = this.GetValueAs<T>();
        return result == null ? defaultValue : result;
    }

    /// <summary>
    /// 检查值是否为空.
    /// </summary>
    /// <returns>是否为空.</returns>
    public bool IsEmpty()
    {
        if (this.value == null)
        {
            return true;
        }

        if (this.valueType == typeof(string))
        {
            return string.IsNullOrEmpty(this.value?.ToString());
        }

        return false;
    }

    /// <summary>
    /// 创建参数的深拷贝.
    /// </summary>
    /// <returns>参数的副本.</returns>
    public OutputParam Clone()
    {
        return new OutputParam
        {
            name = this.name,
            valueType = this.valueType,
            value = this.value,
            description = this.description,
            isReadOnly = this.isReadOnly
        };
    }

    /// <summary>
    /// 返回参数的字符串表示.
    /// </summary>
    /// <returns>参数信息.</returns>
    public override string ToString()
    {
        var valueStr = this.value?.ToString() ?? "null";
        return $"{this.name} ({this.valueType.Name}): {valueStr}";
    }

    /// <summary>
    /// 获取参数的详细信息.
    /// </summary>
    /// <returns>详细信息字符串.</returns>
    public string GetDetailInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"参数名称: {this.name}");
        sb.AppendLine($"参数类型: {this.valueType.FullName}");
        sb.AppendLine($"参数值: {this.value ?? "null"}");
        sb.AppendLine($"描述: {this.description ?? "无"}");
        sb.AppendLine($"只读: {this.isReadOnly}");
        return sb.ToString();
    }
}