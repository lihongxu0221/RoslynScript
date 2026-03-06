using System.ComponentModel;

namespace BgCommon.Script.Compiler.Models;

/// <summary>
/// 表示脚本输入参数的类,包含参数名称、类型、值及默认值信息.
/// </summary>
[Serializable]
public partial class InputParam : ObservableObject
{
    private string name = string.Empty;
    private Type valueType = typeof(string);
    private object? value;
    private object? defaultValue;
    private bool isRequired;
    private string description = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputParam"/> class.
    /// </summary>
    public InputParam()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputParam"/> class.
    /// </summary>
    /// <param name="name">参数名称.</param>
    /// <param name="valueType">参数值类型.</param>
    /// <param name="value">参数值.</param>
    public InputParam(string name, Type valueType, object? value = null)
    {
        this.Name = name;
        this.ValueType = valueType;
        this.Value = value;
        this.DefaultValue = value;
    }

    /// <summary>
    /// Gets or sets 参数名称.
    /// </summary>
    public string Name
    {
        get => this.name;
        set => this.SetProperty(ref this.name, value);
    }

    /// <summary>
    /// Gets or sets 参数值类型.
    /// </summary>
    public Type ValueType
    {
        get => this.valueType;
        set => this.SetProperty(ref this.valueType, value);
    }

    /// <summary>
    /// Gets or sets 参数值.
    /// </summary>
    public object? Value
    {
        get => this.value;
        set => this.SetProperty(ref this.value, value);
    }

    /// <summary>
    /// Gets or sets 参数默认值.
    /// </summary>
    public object? DefaultValue
    {
        get => this.defaultValue;
        set => this.SetProperty(ref this.defaultValue, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether 是否为必填参数.
    /// </summary>
    public bool IsRequired
    {
        get => this.isRequired;
        set => this.SetProperty(ref this.isRequired, value);
    }

    /// <summary>
    /// Gets or sets 参数描述信息.
    /// </summary>
    public string Description
    {
        get => this.description;
        set => this.SetProperty(ref this.description, value);
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
    /// 验证参数是否有效.
    /// </summary>
    /// <returns>验证结果.</returns>
    public bool Validate()
    {
        // 必填参数不能为空
        if (this.isRequired && (this.value == null || (this.valueType == typeof(string) && string.IsNullOrEmpty(this.value?.ToString()))))
        {
            return false;
        }

        // 类型检查(非空值时)
        if (this.value != null && !this.valueType.IsAssignableFrom(this.value.GetType()))
        {
            // 尝试类型转换
            try
            {
                Convert.ChangeType(this.value, this.valueType);
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 重置参数值为默认值.
    /// </summary>
    public void Reset()
    {
        this.value = this.defaultValue;
    }

    /// <summary>
    /// 创建参数的深拷贝.
    /// </summary>
    /// <returns>参数的副本.</returns>
    public InputParam Clone()
    {
        return new InputParam
        {
            Name = this.Name,
            ValueType = this.ValueType,
            Value = this.Value,
            DefaultValue = this.DefaultValue,
            IsRequired = this.IsRequired,
            Description = this.Description,
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
}