namespace BgCommon.Text.Json.Converters;

/// <summary>
/// 可空日期格式 Json 转换器，支持自定义格式化字符串.
/// </summary>
public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    /// <summary>
    /// 存储日期格式化字符串.
    /// </summary>
    private readonly string format;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableDateTimeJsonConverter"/> class.
    /// </summary>
    public NullableDateTimeJsonConverter()
        : this("yyyy-MM-dd HH:mm:ss")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableDateTimeJsonConverter"/> class.
    /// </summary>
    /// <param name="format">指定的日期格式字符串.</param>
    public NullableDateTimeJsonConverter(string format)
    {
        // 按照规则 8 使用内置的空检查方法
        ArgumentNullException.ThrowIfNull(format, nameof(format));
        this.format = format;
    }

    /// <summary>
    /// 从 Json 数据中读取并解析可空日期.
    /// </summary>
    /// <param name="reader">当前的 Json 读取器.</param>
    /// <param name="typeToConvert">目标转换类型.</param>
    /// <param name="options">序列化配置选项.</param>
    /// <returns>解析后的日期时间对象，若解析失败则返回日期最小值.</returns>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 如果当前令牌是字符串类型
        if (reader.TokenType == JsonTokenType.String)
        {
            string? dateString = reader.GetString();
            return Extensions.ToLocalTime(BgConvert.ToDateTime(dateString));
        }

        // 尝试按标准日期格式获取值
        if (reader.TryGetDateTime(out DateTime dateValue))
        {
            return Extensions.ToLocalTime(dateValue);
        }

        // 默认返回日期最小值
        return DateTime.MinValue;
    }

    /// <summary>
    /// 将可空日期对象写入到 Json 数据流中.
    /// </summary>
    /// <param name="writer">当前的 Json 写入器.</param>
    /// <param name="value">待写入的日期时间值.</param>
    /// <param name="options">序列化配置选项.</param>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        // 如果值为 null 则写入 Json 的 null 值
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // 转换为本地时间并根据构造函数指定的格式输出字符串
        string formattedDate = Extensions.ToLocalTime(value.Value).ToString(this.format);
        writer.WriteStringValue(formattedDate);
    }
}