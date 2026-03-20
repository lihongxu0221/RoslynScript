using BgCommon.Localization;
using BgCommon.Localization.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace BgCommon;

/// <summary>
/// 提供枚举类型的扩展方法，用于获取值、名称、多语言模型及转换操作.
/// </summary>
public static partial class EnumModelExtensions
{
    /// <summary>
    /// 获取枚举项对应的整数值.
    /// </summary>
    /// <param name="instance">枚举实例.</param>
    /// <returns>返回枚举对应的整数值，如果实例为空则返回 null.</returns>
    public static int? Value(this System.Enum instance)
    {
        // 检查实例是否为空.
        if (instance == null)
        {
            return null;
        }

        // 调用静态获取值方法.
        return GetValue(instance.GetType(), instance);
    }

    /// <summary>
    /// 获取枚举项对应的指定类型返回值.
    /// </summary>
    /// <typeparam name="TResult">预期的返回值类型.</typeparam>
    /// <param name="instance">枚举实例.</param>
    /// <returns>转换后的目标类型结果.</returns>
    public static TResult? Value<TResult>(this System.Enum instance)
    {
        // 检查实例是否为空.
        if (instance == null)
        {
            return default;
        }

        // 将整数值转换为目标泛型类型.
        return BgConvert.To<TResult>(instance.Value());
    }

    /// <summary>
    /// 获取指定枚举类型的所有项并封装为可观察的实体列表.
    /// </summary>
    /// <param name="enumType">枚举的类型信息.</param>
    /// <returns>包含枚举详细信息的模型集合.</returns>
    public static ObservableCollection<EnumModel> GetEnumModels(this Type? enumType)
    {
        // 创建返回的结果集合.
        ObservableCollection<EnumModel> enumModelCollection = new ObservableCollection<EnumModel>();

        // 校验输入类型是否为空.
        ArgumentNullException.ThrowIfNull(enumType, nameof(enumType));

        // 验证传入的类型必须是枚举.
        if (!enumType.IsEnum)
        {
            throw new ArgumentException($"Type must be an Enum, but is {enumType.Name}.");
        }

        // 获取枚举类型中的所有值并进行遍历转换.
        Array enumValues = Enum.GetValues(enumType);
        foreach (Enum enumValue in enumValues)
        {
            EnumModel? enumInfoModel = enumValue.GetEnumModel();
            if (enumInfoModel != null)
            {
                enumModelCollection.Add(enumInfoModel);
            }
        }

        return enumModelCollection;
    }

    /// <summary>
    /// 获取单个枚举项的详细描述模型，包含多语言显示文本及启用状态.
    /// </summary>
    /// <param name="value">枚举项值.</param>
    /// <returns>封装了显示名、描述及多语言键的实体模型.</returns>
    public static EnumModel GetEnumModel(this Enum value)
    {
        // 初始化枚举实体类并设置原始值.
        EnumModel enumInfoModel = new EnumModel();
        enumInfoModel.Value = value;

        Type currentEnumType = value.GetType();
        FieldInfo? enumFieldInfo = currentEnumType.GetField(value.ToString());

        if (enumFieldInfo != null)
        {
            // 设置默认的基础信息.
            enumInfoModel.Name = enumFieldInfo.Name;
            enumInfoModel.Display = string.Empty;
            enumInfoModel.Description = value.ToString();

            // 获取并设置启用状态特性.
            EnableAttribute? enableAttr = enumFieldInfo.GetCustomAttributes(typeof(EnableAttribute), false)
                                                  .FirstOrDefault() as EnableAttribute;
            enumInfoModel.IsEnable = enableAttr == null || enableAttr.IsEnabled;

            // 获取多语言提供者并尝试解析显示名称与描述.
            ILocalizationProvider? localizationProvider = LocalizationProviderFactory.GetInstance();
            if (localizationProvider != null)
            {
                // 优先尝试从 DisplayAttribute 获取.
                DisplayAttribute? displayAttr = enumFieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                                                          .FirstOrDefault() as DisplayAttribute;
                if (displayAttr != null)
                {
                    enumInfoModel.LangKey = displayAttr.Name!;
                    enumInfoModel.Display = localizationProvider.GetString(displayAttr.Name!)?.Replace("|", Environment.NewLine)!;
                    enumInfoModel.Description = localizationProvider.GetString(displayAttr.Description!)?.Replace("|", Environment.NewLine)!;
                }
                else
                {
                    // 备选尝试从 DisplayNameAttribute 获取.
                    DisplayNameAttribute? displayNameAttr = enumFieldInfo.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                                                  .FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttr != null)
                    {
                        enumInfoModel.LangKey = displayNameAttr.DisplayName;
                        enumInfoModel.Display = localizationProvider.GetString(displayNameAttr.DisplayName!)?.Replace("|", Environment.NewLine)!;
                    }
                }
            }
        }

        return enumInfoModel;
    }

    /// <summary>
    /// 获取枚举项对应的多语言显示文本.
    /// </summary>
    /// <param name="value">枚举项值.</param>
    /// <returns>多语言文本，若解析失败则返回枚举项的原始字符串.</returns>
    public static string GetLocalizationString(this Enum value)
    {
        // 尝试获取实体模型并提取其显示文本.
        return value.GetEnumModel()?.Display ?? value.ToString();
    }

    /// <summary>
    /// 将成员名称或数值转换为指定的枚举项.
    /// </summary>
    /// <typeparam name="TEnum">目标枚举类型.</typeparam>
    /// <param name="member">成员名、字符串值或对象值.</param>
    /// <returns>解析后的枚举项.</returns>
    public static TEnum? Parse<TEnum>(object member)
        where TEnum : struct
    {
        // 校验输入对象并转换为安全字符串.
        string memberStringValue = member.SafeString();
        if (string.IsNullOrWhiteSpace(memberStringValue))
        {
            if (typeof(TEnum).IsGenericType)
            {
                return default;
            }

            // 按照规则 8 抛出参数空异常.
            ArgumentNullException.ThrowIfNull(member, nameof(member));
        }

        // 执行枚举解析，忽略大小写.
        if (System.Enum.TryParse<TEnum>(value: memberStringValue, true, out var value))
        {
            return value;
        }

        return default(TEnum);
    }

    /// <summary>
    /// 获取枚举成员的名称.
    /// </summary>
    /// <typeparam name="TEnum">枚举类型.</typeparam>
    /// <param name="member">枚举项实例、名称或数值.</param>
    /// <returns>枚举成员的名称字符串.</returns>
    public static string GetName<TEnum>(object member)
    {
        // 调用类型重载方法.
        return GetName(Common.GetType<TEnum>(), member);
    }

    /// <summary>
    /// 获取指定枚举类型中成员的名称.
    /// </summary>
    /// <param name="type">枚举的类型信息.</param>
    /// <param name="member">成员对象.</param>
    /// <returns>成员名称，如果失败则返回空字符串.</returns>
    public static string GetName(Type type, object member)
    {
        // 校验输入参数是否为空.
        if (type == null || member == null)
        {
            return string.Empty;
        }

        // 如果已经是字符串则直接返回.
        if (member is string memberName)
        {
            return memberName;
        }

        // 验证是否为枚举类型.
        if (type.GetTypeInfo().IsEnum == false)
        {
            return string.Empty;
        }

        // 从系统枚举库获取名称.
        return System.Enum.GetName(type, member) ?? string.Empty;
    }

    /// <summary>
    /// 获取枚举成员对应的整数值.
    /// </summary>
    /// <typeparam name="TEnum">枚举类型.</typeparam>
    /// <param name="member">成员名或项.</param>
    /// <returns>对应的整数值.</returns>
    public static int? GetValue<TEnum>(object member)
    {
        // 调用类型重载方法.
        return GetValue(Common.GetType<TEnum>(), member);
    }

    /// <summary>
    /// 获取指定枚举类型中成员的整数值.
    /// </summary>
    /// <param name="type">枚举的类型信息.</param>
    /// <param name="member">成员名称或实例.</param>
    /// <returns>整数值结果，若解析失败返回 null.</returns>
    public static int? GetValue(Type type, object member)
    {
        // 转换输入为安全字符串.
        string memberStringValue = member.SafeString();
        if (string.IsNullOrEmpty(memberStringValue))
        {
            return null;
        }

        // 解析并转换为可空整数.
        object parsedObjectValue = System.Enum.Parse(type, memberStringValue, true);
        return BgConvert.To<int?>(parsedObjectValue);
    }

    /// <summary>
    /// 获取指定枚举类型中所有成员的名称集合.
    /// </summary>
    /// <typeparam name="TEnum">枚举类型.</typeparam>
    /// <returns>成员名称列表.</returns>
    public static List<string> GetNames<TEnum>()
    {
        // 调用类型重载方法.
        return GetNames(typeof(TEnum));
    }

    /// <summary>
    /// 获取指定枚举类型中所有成员的名称集合.
    /// </summary>
    /// <param name="type">枚举的类型信息.</param>
    /// <returns>成员名称列表.</returns>
    public static List<string> GetNames(Type type)
    {
        // 获取实际类型处理可空类型.
        Type actualEnumType = Common.GetType(type);
        if (actualEnumType.IsEnum == false)
        {
            // 通过多语言工厂获取错误提示并抛出异常.
            var localizedErrorMessage = LocalizationProviderFactory.GetString("TypeNotEnum", actualEnumType);
            throw new InvalidOperationException(localizedErrorMessage);
        }

        List<string> enumNameList = new List<string>();

        // 遍历类型字段并筛选出枚举项字段.
        foreach (FieldInfo enumField in actualEnumType.GetFields())
        {
            if (!enumField.FieldType.IsEnum)
            {
                continue;
            }

            enumNameList.Add(enumField.Name);
        }

        return enumNameList;
    }
}