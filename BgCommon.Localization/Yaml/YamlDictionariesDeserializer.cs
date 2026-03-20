namespace BgCommon.Localization;

/// <summary>
/// YAML 字典反序列化器，用于将类 YAML 格式的字符串解析为多级命名空间的字典结构.
/// </summary>
public static class YamlDictionariesDeserializer
{
    /// <summary>
    /// 默认命名空间标识字符串.
    /// </summary>
    private const string DefaultNamespace = "default";

    /// <summary>
    /// 用于分割行字符串的换行符字符数组.
    /// </summary>
    private static readonly char[] Separator = new char[] { '\n', '\r' };

    /// <summary>
    /// 将输入的 YAML 格式字符串反序列化为双层嵌套字典.
    /// </summary>
    /// <param name="input"> 待解析的 YAML 格式输入字符串. </param>
    /// <returns> 返回按命名空间划分的键值对字典集合. </returns>
    public static Dictionary<string, Dictionary<string, string>> FromString(string input)
    {
        // 按照规则 8 使用内置的空检查方法.
        ArgumentNullException.ThrowIfNull(input, nameof(input));

        var resultDictionary = new Dictionary<string, Dictionary<string, string>>();

        // 按换行符分割字符串并移除空行.
        string[] contentLines = input.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        string currentNamespace = DefaultNamespace;
        int currentIndentation = 0;

        foreach (string line in contentLines)
        {
            string trimmedLine = line.Trim();

            // 忽略 YAML 中的注释行.
            if (trimmedLine.StartsWith('#'))
            {
                continue;
            }

            // 检查是否为命名空间声明（以冒号结尾且无值）.
            if (trimmedLine.EndsWith(':'))
            {
                currentNamespace = trimmedLine.TrimEnd(':');
                currentIndentation = line.IndexOf(trimmedLine);
                continue;
            }

            // 检查是否为特殊的嵌套命名空间标识（以 "- " 开头）.
            if (trimmedLine.StartsWith("- "))
            {
                currentNamespace += "." + trimmedLine.TrimStart('-').Trim();
                continue;
            }

            // 如果当前行的缩进小于当前命名空间的缩进，则认为命名空间已结束，重置回默认命名空间.
            if (line.IndexOf(trimmedLine) < currentIndentation)
            {
                currentNamespace = DefaultNamespace;
                currentIndentation = 0;
            }

            // 拆分键和值，限制仅拆分为两部分.
            string[] keyValueParts = trimmedLine.Split(':', 2).Select(part => part.Trim()).ToArray();

            if (keyValueParts.Length != 2)
            {
                continue;
            }

            // 清理值两端的引号.
            string sanitizedValue = RemoveStartEndQuotes(keyValueParts[1]);

            // 将解析出的键值对添加到结果字典中.
            AddToDictionary(resultDictionary, currentNamespace, keyValueParts[0], sanitizedValue);
        }

        return resultDictionary;
    }

    /// <summary>
    /// 将键值对存入目标字典，并根据逻辑处理默认命名空间的映射.
    /// </summary>
    /// <param name="targetDictionary"> 存储结果的目标字典对象. </param>
    /// <param name="namespaceKey"> 当前所属的命名空间键名. </param>
    /// <param name="itemKey"> 配置项的键名. </param>
    /// <param name="itemValue"> 配置项的值内容. </param>
    private static void AddToDictionary(
        Dictionary<string, Dictionary<string, string>> targetDictionary,
        string namespaceKey,
        string itemKey,
        string itemValue)
    {
        // 如果目标命名空间不存在，则初始化对应的字典.
        if (!targetDictionary.TryGetValue(namespaceKey, out Dictionary<string, string>? innerDictionary))
        {
            innerDictionary = new Dictionary<string, string>();
            targetDictionary[namespaceKey] = innerDictionary;
        }

        innerDictionary[itemKey] = itemValue;

        // 如果当前不是嵌套命名空间（不包含点号），则将其同时也存入默认命名空间中.
        if (!namespaceKey.Contains('.'))
        {
            if (!targetDictionary.TryGetValue(DefaultNamespace, out Dictionary<string, string>? defaultDictionary))
            {
                defaultDictionary = new Dictionary<string, string>();
                targetDictionary[DefaultNamespace] = defaultDictionary;
            }

            defaultDictionary[itemKey] = itemValue;
        }
    }

    /// <summary>
    /// 移除字符串首尾对应的单引号或双引号.
    /// </summary>
    /// <param name="rawValue"> 待处理的原始字符串. </param>
    /// <returns> 移除引号后的字符串结果. </returns>
    private static string RemoveStartEndQuotes(string rawValue)
    {
        // 检查字符串是否由匹配的单引号或双引号包裹.
        if ((rawValue.StartsWith('\'') && rawValue.EndsWith('\'')) ||
            (rawValue.StartsWith('\"') && rawValue.EndsWith('\"')))
        {
            // 截取中间部分内容.
            if (rawValue.Length >= 2)
            {
                return rawValue.Substring(1, rawValue.Length - 2);
            }
        }

        return rawValue;
    }
}