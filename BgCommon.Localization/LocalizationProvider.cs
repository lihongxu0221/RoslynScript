namespace BgCommon.Localization;

/// <summary>
/// 本地化提供程序类，负责管理本地化资源集并提供多语言字符串查询功能.
/// </summary>
public class LocalizationProvider : ILocalizationProvider
{
    /// <summary>
    /// 存储所有的本地化资源集合.
    /// </summary>
    private readonly IEnumerable<LocalizationSet> localizationSets;

    /// <summary>
    /// 当前使用的语言区域信息.
    /// </summary>
    private CultureInfo currentCulture;

    /// <summary>
    /// 当当前语言区域发生更改时触发的事件.
    /// </summary>
    public event EventHandler<CultureInfo>? CultureChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationProvider"/> class.
    /// </summary>
    /// <param name="currentCulture"> 初始的语言区域信息. </param>
    /// <param name="localizationSets"> 初始的本地化资源集合. </param>
    public LocalizationProvider(CultureInfo currentCulture, IEnumerable<LocalizationSet> localizationSets)
    {
        // 验证输入参数是否为空
        ArgumentNullException.ThrowIfNull(currentCulture, nameof(currentCulture));
        ArgumentNullException.ThrowIfNull(localizationSets, nameof(localizationSets));

        this.currentCulture = currentCulture;
        this.localizationSets = localizationSets;
    }

    /// <summary>
    /// 获取当前生效的语言区域信息.
    /// </summary>
    /// <returns> 返回当前的 <see cref="CultureInfo"/>. </returns>
    public CultureInfo GetCulture()
    {
        return this.currentCulture;
    }

    /// <summary>
    /// 获取指定语言区域的所有本地化资源集.
    /// </summary>
    /// <param name="culture"> 目标语言区域. </param>
    /// <returns> 返回匹配的本地化资源集合. </returns>
    public IEnumerable<LocalizationSet> GetLocalizationSets(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture, nameof(culture));

        // 根据语言区域过滤集合
        return this.localizationSets.Where(s => s.Culture.Equals(culture));
    }

    /// <summary>
    /// 获取指定语言区域和名称的特定本地化资源集.
    /// </summary>
    /// <param name="culture"> 目标语言区域. </param>
    /// <param name="name"> 资源集名称. </param>
    /// <returns> 返回匹配的资源集，若未找到则返回 null. </returns>
    public LocalizationSet? GetLocalizationSet(CultureInfo culture, string? name)
    {
        ArgumentNullException.ThrowIfNull(culture, nameof(culture));

        // 如果未指定名称，则返回该语言下的第一个资源集
        if (name is null)
        {
            return this.localizationSets.FirstOrDefault(s => s.Culture.Equals(culture));
        }

        // 返回语言和名称均匹配的资源集
        return this.localizationSets.FirstOrDefault(s => s.Culture.Equals(culture) && s.Name == name);
    }

    /// <summary>
    /// 获取指定键的本地化字符串.
    /// </summary>
    /// <param name="key"> 资源键名. </param>
    /// <param name="assembleyName"> 程序集名称标识. </param>
    /// <returns> 返回查找到的本地化文本，若未找到则返回键名本身. </returns>
    public string GetString(string key, string? assembleyName)
    {
        return this.GetString(key, this.GetCulture(), assembleyName);
    }

    /// <summary>
    /// 获取指定键在特定语言下的本地化字符串.
    /// </summary>
    /// <param name="key"> 资源键名. </param>
    /// <param name="cultureInfo"> 指定的语言区域. </param>
    /// <param name="assembleyName"> 程序集名称标识. </param>
    /// <returns> 返回查找到的本地化文本. </returns>
    public string GetString(string key, CultureInfo cultureInfo, string? assembleyName)
    {
        // 获取指定语言的所有资源集
        IEnumerable<LocalizationSet> availableSets = this.GetLocalizationSets(cultureInfo);

        if (availableSets != null)
        {
            foreach (LocalizationSet? activeSet in availableSets)
            {
                // 校验资源集是否适用并包含指定的键
                if (activeSet != null && activeSet.ContainKey(key) && activeSet.IsApplicableTo(assembleyName))
                {
                    string? translatedValue = activeSet[key];
                    if (!string.IsNullOrEmpty(translatedValue))
                    {
                        return translatedValue;
                    }
                }
            }
        }

        // 若未找到匹配项，则返回原始键名
        return key;
    }

    /// <summary>
    /// 获取指定键在多个语言下的本地化字符串数组.
    /// </summary>
    /// <param name="key"> 资源键名. </param>
    /// <param name="assembleyName"> 程序集名称标识. </param>
    /// <param name="cultureInfos"> 语言区域数组. </param>
    /// <returns> 返回包含多个翻译结果的数组. </returns>
    public string[] GetStrings(string key, string? assembleyName, params CultureInfo[] cultureInfos)
    {
        List<string> resultList = new List<string>();

        // 遍历所有请求的语言环境
        for (int i = 0; i < cultureInfos.Length; i++)
        {
            IEnumerable<LocalizationSet> targetSets = this.GetLocalizationSets(cultureInfos[i]);
            if (targetSets != null)
            {
                foreach (LocalizationSet? targetSet in targetSets)
                {
                    // 匹配程序集范围和键名
                    if (targetSet != null && targetSet.ContainKey(key) && targetSet.IsApplicableTo(assembleyName))
                    {
                        string? localizedText = targetSet[key];
                        if (string.IsNullOrEmpty(localizedText))
                        {
                            localizedText = key;
                        }

                        resultList.Add(localizedText);
                    }
                }
            }
        }

        string[] finalResults = resultList.ToArray();
        resultList.Clear();
        return finalResults;
    }

    /// <summary>
    /// 设置当前的语言区域并触发更改事件.
    /// </summary>
    /// <param name="cultureInfo"> 新的语言区域信息. </param>
    public void SetCulture(CultureInfo cultureInfo)
    {
        ArgumentNullException.ThrowIfNull(cultureInfo, nameof(cultureInfo));

        // 判断语言是否确实发生了变化
        bool hasCultureChanged = !this.currentCulture.Equals(cultureInfo);
        this.currentCulture = cultureInfo;

        if (hasCultureChanged)
        {
            // 触发通知
            this.CultureChanged?.Invoke(this, this.currentCulture);
        }
    }
}