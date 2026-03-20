using System.Text.RegularExpressions;

namespace BgCommon.Helpers;

/// <summary>
/// 正则表达式操作辅助类，提供匹配、提取、分割和替换等常用功能.
/// </summary>
public static class RegexHelper
{
    /// <summary>
    /// 根据正则表达式模式获取匹配值的字典集合.
    /// </summary>
    /// <param name="input">待匹配的输入字符串.</param>
    /// <param name="pattern">正则表达式模式字符串.</param>
    /// <param name="resultPatterns">结果提取模式数组，例如：new[]{"$1","$2"}.</param>
    /// <param name="options">正则表达式选项，默认为忽略大小写.</param>
    /// <returns>包含提取结果的字典，键为提取模式，值为匹配到的内容.</returns>
    public static Dictionary<string, string> GetValues(string input, string pattern, string[] resultPatterns, RegexOptions options = RegexOptions.IgnoreCase)
    {
        Dictionary<string, string> matchedValuesDictionary = new Dictionary<string, string>();

        // 验证输入内容是否为空
        if (string.IsNullOrWhiteSpace(input))
        {
            return matchedValuesDictionary;
        }

        // 执行正则匹配
        Match regexMatch = System.Text.RegularExpressions.Regex.Match(input, pattern, options);
        if (regexMatch.Success == false)
        {
            return matchedValuesDictionary;
        }

        // 将匹配结果填充到字典中
        AddResults(matchedValuesDictionary, regexMatch, resultPatterns);
        return matchedValuesDictionary;
    }

    /// <summary>
    /// 获取正则表达式匹配到的单个值.
    /// </summary>
    /// <param name="input">待匹配的输入字符串.</param>
    /// <param name="pattern">正则表达式模式字符串.</param>
    /// <param name="resultPattern">结果提取模式，例如："$1" 用于获取第一个捕获组的值.</param>
    /// <param name="options">正则表达式选项，默认为忽略大小写.</param>
    /// <returns>匹配到的字符串结果，若未匹配则返回空字符串.</returns>
    public static string GetValue(string input, string pattern, string resultPattern = "", RegexOptions options = RegexOptions.IgnoreCase)
    {
        // 验证输入内容是否为空
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // 执行正则匹配
        Match regexMatch = System.Text.RegularExpressions.Regex.Match(input, pattern, options);
        if (regexMatch.Success == false)
        {
            return string.Empty;
        }

        // 如果未指定提取模式，则返回完整匹配值，否则按模式提取
        return string.IsNullOrWhiteSpace(resultPattern) ? regexMatch.Value : regexMatch.Result(resultPattern);
    }

    /// <summary>
    /// 使用正则表达式将字符串分割为数组.
    /// </summary>
    /// <param name="input">待分割的输入字符串.</param>
    /// <param name="pattern">正则表达式分割模式.</param>
    /// <param name="options">正则表达式选项.</param>
    /// <returns>分割后的字符串数组.</returns>
    public static string[] Split(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<string>();
        }

        return System.Text.RegularExpressions.Regex.Split(input, pattern, options);
    }

    /// <summary>
    /// 使用正则表达式替换字符串中的内容.
    /// </summary>
    /// <param name="input">待处理的输入字符串.</param>
    /// <param name="pattern">正则表达式匹配模式.</param>
    /// <param name="replacement">用于替换的字符串.</param>
    /// <param name="options">正则表达式选项.</param>
    /// <returns>替换后的字符串.</returns>
    public static string Replace(string input, string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement, options);
    }

    /// <summary>
    /// 验证输入字符串与正则表达式模式是否匹配.
    /// </summary>
    /// <param name="input">待验证的输入字符串.</param>
    /// <param name="pattern">正则表达式模式.</param>
    /// <returns>如果匹配成功则返回 true，否则返回 false.</returns>
    public static bool IsMatch(string input, string pattern)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, pattern);
    }

    /// <summary>
    /// 验证输入字符串与正则表达式模式是否匹配.
    /// </summary>
    /// <param name="input">待验证的输入字符串.</param>
    /// <param name="pattern">正则表达式模式.</param>
    /// <param name="options">正则表达式选项.</param>
    /// <returns>如果匹配成功则返回 true，否则返回 false.</returns>
    public static bool IsMatch(string input, string pattern, RegexOptions options)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, pattern, options);
    }

    /// <summary>
    /// 获取正则表达式的第一个匹配项结果.
    /// </summary>
    /// <param name="input">待匹配的输入字符串.</param>
    /// <param name="pattern">正则表达式模式.</param>
    /// <returns>匹配结果对象.</returns>
    public static Match Match(string input, string pattern)
    {
        return System.Text.RegularExpressions.Regex.Match(input, pattern);
    }

    /// <summary>
    /// 获取正则表达式的第一个匹配项结果.
    /// </summary>
    /// <param name="input">待匹配的输入字符串.</param>
    /// <param name="pattern">正则表达式模式.</param>
    /// <param name="options">正则表达式选项.</param>
    /// <returns>匹配结果对象.</returns>
    public static Match Match(string input, string pattern, RegexOptions options)
    {
        return System.Text.RegularExpressions.Regex.Match(input, pattern, options);
    }

    /// <summary>
    /// 获取正则表达式的所有匹配项集合.
    /// </summary>
    /// <param name="input">待匹配的输入字符串.</param>
    /// <param name="pattern">正则表达式模式.</param>
    /// <returns>匹配项集合.</returns>
    public static MatchCollection Matches(string input, string pattern)
    {
        return System.Text.RegularExpressions.Regex.Matches(input, pattern);
    }

    /// <summary>
    /// 获取正则表达式的所有匹配项集合.
    /// </summary>
    /// <param name="input">待匹配的输入字符串.</param>
    /// <param name="pattern">正则表达式模式.</param>
    /// <param name="options">正则表达式选项.</param>
    /// <returns>匹配项集合.</returns>
    public static MatchCollection Matches(string input, string pattern, RegexOptions options)
    {
        return System.Text.RegularExpressions.Regex.Matches(input, pattern, options);
    }

    /// <summary>
    /// 获取正则表达式的所有匹配项集合并指定超时时间.
    /// </summary>
    /// <param name="input">待匹配的输入字符串.</param>
    /// <param name="pattern">正则表达式模式.</param>
    /// <param name="options">正则表达式选项.</param>
    /// <param name="matchTimeout">匹配操作的超时间隔.</param>
    /// <returns>匹配项集合.</returns>
    public static MatchCollection Matches(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
    {
        return System.Text.RegularExpressions.Regex.Matches(input, pattern, options, matchTimeout);
    }

    /// <summary>
    /// 将正则匹配结果按照指定的模式添加至结果字典中.
    /// </summary>
    /// <param name="matchedValuesDictionary">用于存储结果的字典.</param>
    /// <param name="regexMatch">正则表达式匹配结果对象.</param>
    /// <param name="capturePatterns">结果提取模式字符串数组.</param>
    private static void AddResults(Dictionary<string, string> matchedValuesDictionary, Match regexMatch, string[] capturePatterns)
    {
        // 如果未指定提取模式，则默认存储完整匹配值
        if (capturePatterns == null)
        {
            matchedValuesDictionary.Add(string.Empty, regexMatch.Value);
            return;
        }

        // 遍历模式数组并存储提取后的结果
        foreach (var currentPattern in capturePatterns)
        {
            matchedValuesDictionary.Add(currentPattern, regexMatch.Result(currentPattern));
        }
    }
}