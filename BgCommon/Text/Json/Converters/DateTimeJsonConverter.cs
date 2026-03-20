namespace BgCommon.Text.Json.Converters;

/// <summary>
/// 日期格式Json转换器.
/// </summary>
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// 日期格式.
    /// </summary>
    private readonly string format;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeJsonConverter"/> class.
    /// </summary>
    public DateTimeJsonConverter()
        : this("yyyy-MM-dd HH:mm:ss")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeJsonConverter"/> class.
    /// </summary>
    /// <param name="format">日期格式,默认值: yyyy-MM-dd HH:mm:ss.</param>
    public DateTimeJsonConverter(string format)
    {
        this.format = format;
    }

    /// <inheritdoc/>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return Extensions.ToLocalTime(BgConvert.ToDateTime(reader.GetString()));
        }

        if (reader.TryGetDateTime(out DateTime date))
        {
            return Extensions.ToLocalTime(date);
        }

        return DateTime.MinValue;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        string date = Extensions.ToLocalTime(value).ToString(format);
        writer.WriteStringValue(date);
    }
}