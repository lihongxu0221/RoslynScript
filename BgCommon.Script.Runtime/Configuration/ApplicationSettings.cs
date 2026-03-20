using BgCommon.Core;
using BgCommon.Script.Runtime.DotNet;
using BgCommon.Script.Runtime.Services;
using BgCommon.Script.Runtime.Themes;

namespace BgCommon.Script.Runtime.Configuration;

/// <summary>
/// 应用程序设置实现类，负责配置的持久化与读取.
/// </summary>
internal partial class ApplicationSettings : IApplicationSettings
{
    /// <summary>
    /// 默认配置文件名称.
    /// </summary>
    private const string DefaultConfigFileName = "Script.json";

    /// <summary>
    /// 设置保存的防抖延迟时间（毫秒）.
    /// </summary>
    private const int SaveDelayMilliseconds = 500;

    /// <summary>
    /// JSON 序列化配置选项.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// 遥测提供程序实例接口.
    /// </summary>
    private readonly ITelemetryProvider? telemetryProvider;

    /// <summary>
    /// 当前可序列化的设置数值实例.
    /// </summary>
    private SerializableValues? currentValues;

    /// <summary>
    /// 当前设置文件的路径.
    /// </summary>
    private string? settingsFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationSettings"/> class.
    /// </summary>
    /// <param name="telemetryProvider">遥测提供程序参数.</param>
    public ApplicationSettings(ITelemetryProvider telemetryProvider)
    {
        this.telemetryProvider = telemetryProvider;
        this.InitializeValues(new SerializableValues());
    }

    /// <summary>
    /// Gets 设置数值接口.
    /// </summary>
    public IApplicationSettingsValues Values => this.currentValues!;

    /// <summary>
    /// 初始化设置数值对象并挂载变更事件.
    /// </summary>
    /// <param name="newValue">新的序列化数值对象.</param>
    private void InitializeValues(SerializableValues newValue)
    {
        // 验证新值不为空.
        ArgumentNullException.ThrowIfNull(newValue, nameof(newValue));

        // 如果旧值存在，则解除事件绑定并清理引用.
        if (this.currentValues != null)
        {
            this.currentValues.PropertyChanged -= this.SerializableValues_PropertyChanged;
            this.currentValues.Settings = null;
            this.currentValues = default;
        }

        // 绑定新值并设置双向引用.
        this.currentValues = newValue;
        this.currentValues.PropertyChanged += this.SerializableValues_PropertyChanged;
        this.currentValues.Settings = this;
    }

    /// <summary>
    /// 当设置属性发生变更时的回调处理.
    /// </summary>
    /// <param name="sender">事件源.</param>
    /// <param name="e">属性变更参数.</param>
    private void SerializableValues_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 任何属性改变时自动保存设置.
        this.SaveSettings();
    }

    /// <summary>
    /// 加载默认路径下的设置.
    /// </summary>
    public void LoadDefault()
    {
        // 获取默认文档路径并拼接配置文件名进行加载.
        this.LoadFrom(Path.Combine(this.GetDefaultDocumentPath(), DefaultConfigFileName));
    }

    /// <summary>
    /// 从指定的文件路径加载设置.
    /// </summary>
    /// <param name="path">配置文件路径参数.</param>
    public void LoadFrom(string path)
    {
        // 验证路径参数不为空.
        ArgumentNullException.ThrowIfNull(path, nameof(path));

        try
        {
            // 如果文件不存在，则加载默认设置并保存.
            if (!File.Exists(path))
            {
                this.currentValues.LoadDefaultSettings();
                this.SaveSettings();
                return;
            }

            try
            {
                // 读取 JSON 文本并反序列化.
                var jsonContent = File.ReadAllText(path);
                var deserializedValues = JsonSerializer.Deserialize<SerializableValues>(jsonContent, SerializerOptions) ?? new SerializableValues();
                this.InitializeValues(deserializedValues);
            }
            catch (Exception ex)
            {
                // 发生异常时回退到默认设置并上报错误.
                this.currentValues.LoadDefaultSettings();
                this.telemetryProvider?.ReportError(ex);
            }
        }
        finally
        {
            // 记录当前使用的设置路径.
            this.settingsFilePath = path;
        }
    }

    /// <summary>
    /// 获取默认的文档存储路径.
    /// </summary>
    /// <returns>返回路径字符串.</returns>
    public string GetDefaultDocumentPath()
    {
        // 从全局配置获取基础路径.
        string? baseConfigPath = FileNames.ConfigPath;
        if (string.IsNullOrEmpty(baseConfigPath))
        {
            // 如果无法定位路径，回退到根目录并记录错误.
            baseConfigPath = "/";
            this.telemetryProvider?.ReportError(new InvalidOperationException("无法定位用户文档文件夹，使用根目录替代."));
        }

        return Path.Combine(baseConfigPath);
    }

    /// <summary>
    /// 将当前设置保存到物理文件中.
    /// </summary>
    private void SaveSettings()
    {
        // 如果路径尚未确定，则不执行保存.
        if (this.settingsFilePath == null)
        {
            return;
        }

        try
        {
            // 创建文件流并序列化对象.
            using FileStream fileStream = File.Create(this.settingsFilePath);
            JsonSerializer.Serialize(fileStream, this.currentValues, SerializerOptions);
        }
        catch (Exception ex)
        {
            // 保存失败时记录遥测错误.
            this.telemetryProvider?.ReportError(ex);
        }
    }

    /// <summary>
    /// 可序列化的设置数值内部类.
    /// </summary>
    private partial class SerializableValues : ObservableObject, IApplicationSettingsValues
    {
        private const int LiveModeDelayMsDefault = 2000;
        private const int DefaultFontSize = 12;

        private bool optimizeCompilation;
        private bool formatDocumentOnComment = true;
        private bool sendErrors;
        private bool enableBraceCompletion = true;
        private string? latestVersion = string.Empty;
        private double editorFontSize = DefaultFontSize;
        private string editorFontFamily = GetDefaultPlatformFontFamily();
        private double outputFontSize = DefaultFontSize;
        private bool searchFileContents;
        private bool searchUsingRegex;
        private bool searchWhileTyping;
        private string defaultPlatformName = string.Empty;
        private string? customThemePath = string.Empty;
        private ThemeType? customThemeType;
        private string dotNetSdkDirectoryPath = string.Empty;
        private DotNetFrameworkVersion frameworkVersion = DotNetFrameworkVersion.DotNet8;

        /// <summary>
        /// Gets or sets a value indicating whether 是否发送错误报告.
        /// </summary>
        public bool SendErrors
        {
            get => this.sendErrors;
            set => this.SetProperty(ref this.sendErrors, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether 是否启用括号补全.
        /// </summary>
        public bool EnableBraceCompletion
        {
            get => this.enableBraceCompletion;
            set => this.SetProperty(ref this.enableBraceCompletion, value);
        }

        /// <summary>
        /// Gets or sets 最新版本号.
        /// </summary>
        public string? LatestVersion
        {
            get => this.latestVersion;
            set => this.SetProperty(ref this.latestVersion, value);
        }

        /// <summary>
        /// Gets or sets 编辑器字体大小.
        /// </summary>
        public double EditorFontSize
        {
            get => this.editorFontSize;
            set => this.SetProperty(ref this.editorFontSize, value);
        }

        /// <summary>
        /// Gets or sets 编辑器字体系列.
        /// </summary>
        public string EditorFontFamily
        {
            get => this.editorFontFamily;
            set => this.SetProperty(ref this.editorFontFamily, value);
        }

        /// <summary>
        /// Gets or sets 输出字体大小.
        /// </summary>
        public double OutputFontSize
        {
            get => this.outputFontSize;
            set => this.SetProperty(ref this.outputFontSize, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether 是否搜索文件内容.
        /// </summary>
        public bool SearchFileContents
        {
            get => this.searchFileContents;
            set => this.SetProperty(ref this.searchFileContents, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether 是否使用正则搜索.
        /// </summary>
        public bool SearchUsingRegex
        {
            get => this.searchUsingRegex;
            set => this.SetProperty(ref this.searchUsingRegex, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether 是否优化编译.
        /// </summary>
        public bool OptimizeCompilation
        {
            get => this.optimizeCompilation;
            set => this.SetProperty(ref this.optimizeCompilation, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether 是否在键入时搜索.
        /// </summary>
        public bool SearchWhileTyping
        {
            get => this.searchWhileTyping;
            set => this.SetProperty(ref this.searchWhileTyping, value);
        }

        /// <summary>
        /// Gets or sets 默认平台名称.
        /// </summary>
        public string DefaultPlatformName
        {
            get => this.defaultPlatformName;
            set => this.SetProperty(ref this.defaultPlatformName, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether 是否在注释时格式化文档.
        /// </summary>
        public bool FormatDocumentOnComment
        {
            get => this.formatDocumentOnComment;
            set => this.SetProperty(ref this.formatDocumentOnComment, value);
        }

        /// <summary>
        /// Gets or sets 自定义主题路径.
        /// </summary>
        public string? CustomThemePath
        {
            get => this.customThemePath;
            set => this.SetProperty(ref this.customThemePath, value);
        }

        /// <summary>
        /// Gets or sets 自定义主题类型.
        /// </summary>
        public ThemeType? CustomThemeType
        {
            get => this.customThemeType;
            set => this.SetProperty(ref this.customThemeType, value);
        }

        /// <summary>
        /// Gets or sets Sdk所在路径.
        /// </summary>
        public string DotNetSdkDirectoryPath
        {
            get => this.dotNetSdkDirectoryPath;
            set => this.SetProperty(ref this.dotNetSdkDirectoryPath, value);
        }

        /// <summary>
        /// Gets or sets DotNet 版本号.
        /// </summary>
        public DotNetFrameworkVersion FrameworkVersion
        {
            get => this.frameworkVersion;
            set => this.SetProperty(ref this.frameworkVersion, value);
        }

        /// <summary>
        /// Gets or sets 设置服务的父级引用接口.
        /// </summary>
        [JsonIgnore]
        public ApplicationSettings? Settings { get; set; }

        /// <summary>
        /// 加载默认设置数值.
        /// </summary>
        public void LoadDefaultSettings()
        {
            // 恢复各项默认值.
            this.SendErrors = true;
            this.FormatDocumentOnComment = true;
            this.EditorFontSize = DefaultFontSize;
            this.OutputFontSize = DefaultFontSize;
            this.EditorFontFamily = GetDefaultPlatformFontFamily();
        }

        /// <summary>
        /// 根据不同操作系统获取默认字体系列.
        /// </summary>
        /// <returns>返回字体名称字符串.</returns>
        private static string GetDefaultPlatformFontFamily()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows 平台默认字体.
                return "Cascadia Code,Consolas";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS 平台默认字体.
                return "Menlo";
            }
            else
            {
                // 其他（如 Linux）默认字体.
                return "Monospace";
            }
        }
    }
}