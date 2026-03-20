using BgCommon.Localization.IO;

namespace BgCommon.Localization;

/// <summary>
/// 为 <see cref="LocalizationBuilder"/> 类提供扩展方法，支持从 YAML 资源中加载本地化数据.
/// </summary>
public static partial class LocalizationBuilderExtensions
{
    /// <summary>
    /// 从当前调用程序集的嵌入式资源中加载 YAML 本地化文件.
    /// </summary>
    /// <param name="builder"> 本地化构建器实例. </param>
    /// <param name="path"> 嵌入式资源的路径. </param>
    /// <param name="culture"> 关联的语言区域性信息. </param>
    /// <param name="isPublic"> 标识该本地化资源是否为全局公开. </param>
    /// <returns> 返回更新后的本地化构建器. </returns>
    public static LocalizationBuilder FromYaml(this LocalizationBuilder builder, string path, CultureInfo culture, bool isPublic)
    {
        // 获取调用当前方法的程序集并执行加载逻辑.
        return FromYaml(builder, Assembly.GetCallingAssembly(), path, culture, isPublic);
    }

    /// <summary>
    /// 从指定程序集的嵌入式资源中加载 YAML 本地化文件.
    /// </summary>
    /// <param name="builder"> 本地化构建器实例. </param>
    /// <param name="assembly"> 包含资源的程序集对象. </param>
    /// <param name="path"> 嵌入式资源的路径（需以 .yml 或 .yaml 结尾）. </param>
    /// <param name="culture"> 关联的语言区域性信息. </param>
    /// <param name="isPublic"> 标识该本地化资源是否为全局公开. </param>
    /// <returns> 返回更新后的本地化构建器. </returns>
    /// <exception cref="ArgumentException"> 当路径后缀不符合 YAML 要求时抛出. </exception>
    /// <exception cref="LocalizationBuilderException"> 当资源未找到时抛出. </exception>
    public static LocalizationBuilder FromYaml(this LocalizationBuilder builder, Assembly assembly, string path, CultureInfo culture, bool isPublic)
    {
        // 按照规则 8 验证参数是否为 null.
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(culture, nameof(culture));

        // 校验文件扩展名是否合法.
        if (!(path.EndsWith(".yml") || path.EndsWith(".yaml")))
        {
            throw new ArgumentException($"Parameter {nameof(path)} in {nameof(FromYaml)} must be path to the YAML file.");
        }

        // 读取嵌入式资源的内容.
        string? yamlContents = EmbeddedResourceReader.ReadToEnd(path, assembly);
        if (yamlContents is null)
        {
            throw new LocalizationBuilderException($"Resource {path} not found in assembly {assembly.FullName}.");
        }

        // 反序列化 YAML 字符串为多级字典结构.
        var parsedYamlData = YamlDictionariesDeserializer.FromString(yamlContents);

        foreach (var namespaceEntry in parsedYamlData)
        {
            // 如果命名空间为 null 或为 "default"，则设为默认状态.
            string? resourceName = (namespaceEntry.Key is null || namespaceEntry.Key == "default")
                    ? default
                    : namespaceEntry.Key;

            // 构建用于存储本地化字符串的内部字典.
            var localizedStringDictionary = new Dictionary<string, string?>();
            foreach (KeyValuePair<string, string> keyValue in namespaceEntry.Value)
            {
                if (isPublic)
                {
                    // 公开模式下直接存储键名.
                    localizedStringDictionary.Add(keyValue.Key, keyValue.Value);
                }
                else
                {
                    // 非公开模式下，键名需带上程序集前缀以防止冲突.
                    localizedStringDictionary.Add(
                        $"{assembly.GetName().Name}_{keyValue.Key}",
                        keyValue.Value);
                }
            }

            // 将构建好的本地化集合添加至构建器中.
            builder.AddLocalization(
                new LocalizationSet(
                    assembly,
                    resourceName?.ToLowerInvariant()!,
                    culture,
                    localizedStringDictionary,
                    isPublic));
        }

        return builder;
    }
}