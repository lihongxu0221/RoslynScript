namespace BgCommon.Script;

/// <summary>
/// 提供用于构建和配置 <see cref="ScriptConfig"/> 对象的扩展方法和静态方法.
/// </summary>
public static class ScriptConfigBuilder
{
    /// <summary>
    /// 创建并返回一个新的 <see cref="ScriptConfig"/> 实例.
    /// </summary>
    /// <param name="referLibsPath">引用程序集所在路径.</param>
    /// <param name="scriptPath">脚本文件存储路径.</param>
    /// <param name="templatePath">模板文件存储路径.</param>
    /// <returns>返回初始化后的 <see cref="ScriptConfig"/> 对象.</returns>
    public static ScriptConfig Build(string referLibsPath, string scriptPath, string templatePath)
    {
        // 验证输入路径参数.
        ArgumentNullException.ThrowIfNull(referLibsPath, nameof(referLibsPath));
        ArgumentNullException.ThrowIfNull(scriptPath, nameof(scriptPath));
        ArgumentNullException.ThrowIfNull(templatePath, nameof(templatePath));

        // 实例化新的配置对象并返回.
        return new ScriptConfig(referLibsPath, scriptPath, templatePath);
    }

    /// <summary>
    /// 向脚本配置中添加指定的命名空间.
    /// </summary>
    /// <param name="config">当前的脚本配置实例.</param>
    /// <param name="ns">要添加的命名空间字符串.</param>
    /// <returns>返回更新后的 <see cref="ScriptConfig"/> 实例，支持链式调用.</returns>
    public static ScriptConfig AddNamespace(this ScriptConfig config, string ns)
    {
        // 验证参数.
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        ArgumentNullException.ThrowIfNull(ns, nameof(ns));

        // 将命名空间添加到集合中.
        config.Namespaces.Add(ns);
        return config;
    }

    /// <summary>
    /// 向脚本配置中添加指定的引用程序集.
    /// </summary>
    /// <param name="config">当前的脚本配置实例.</param>
    /// <param name="refLib">要引用的程序集名称或路径.</param>
    /// <returns>返回更新后的 <see cref="ScriptConfig"/> 实例，支持链式调用.</returns>
    public static ScriptConfig AddReferLib(this ScriptConfig config, string refLib)
    {
        // 验证参数.
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        ArgumentNullException.ThrowIfNull(refLib, nameof(refLib));

        // 将程序集库添加到引用列表中.
        config.ReferLibs.Add(refLib);
        return config;
    }

    /// <summary>
    /// 向脚本配置中添加一个新的模板配置项.
    /// </summary>
    /// <param name="config">当前的脚本配置实例.</param>
    /// <param name="templateName">模板名称.</param>
    /// <param name="templatePath">模板所在路径.</param>
    /// <param name="referLibsPath">该模板引用程序集所在路径.</param>
    /// <param name="referLibs">该模板引用的程序集列表.</param>
    /// <returns>返回更新后的 <see cref="ScriptConfig"/> 实例，支持链式调用.</returns>
    public static ScriptConfig AddTemplateConfig(this ScriptConfig config, string templateName, string templatePath, string referLibsPath, params string[] referLibs)
    {
        // 验证参数非空.
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        // 允许模板有自己的扩展名，如果未指定则遵循全局配置
        var template = new ScriptTemplate(
            templateName,
            config.TemplateFileExtension,
            templatePath,
            referLibsPath,
            referLibs);

        config.Templates.Add(template);
        return config;
    }
}