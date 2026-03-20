namespace BgCommon.Text.Json.Converters;

/// <summary>
/// 可空long格式Json转换器.
/// </summary>
public class NullableLongJsonConverter : JsonConverter<long?>
{
    /// <inheritdoc/>
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return BgConvert.ToLongOrNull(reader.GetString());
        }

        return reader.TryGetInt64(out var value) ? value : null;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.SafeString());
    }
}