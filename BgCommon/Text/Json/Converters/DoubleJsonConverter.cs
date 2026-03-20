namespace BgCommon.Text.Json.Converters;

/// <summary>
/// Double数组的JsonConverter，支持NaN、Infinity和-Infinity的序列化和反序列化.
/// </summary>
public class DoubleJsonConverter : JsonConverter<double>
{
    /// <inheritdoc/>
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            return str switch
            {
                "NaN" => double.NaN,
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                _ => double.TryParse(str, out double value) ? value : double.NaN
            };
        }

        return reader.GetDouble();
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNaN(value))
        {
            writer.WriteStringValue("NaN"); // 或 writer.WriteNullValue();
        }
        else if (double.IsPositiveInfinity(value))
        {
            writer.WriteStringValue("Infinity");
        }
        else if (double.IsNegativeInfinity(value))
        {
            writer.WriteStringValue("-Infinity");
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}