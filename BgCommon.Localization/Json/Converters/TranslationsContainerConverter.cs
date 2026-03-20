using BgCommon.Localization.Json.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgCommon.Localization.Json.Converters;

public class TranslationsContainerConverter : JsonConverter<ITranslationsContainer>
{
    public override ITranslationsContainer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonElement jsonObject = JsonDocument.ParseValue(ref reader).RootElement;

        string version = "1.0";

        foreach (JsonProperty property in jsonObject.EnumerateObject())
        {
            if (string.Equals(property.Name, "Version", StringComparison.OrdinalIgnoreCase))
            {
                version = property.Value.GetString() ?? version;
                break;
            }
        }

        return new TranslationsContainer(new Version(version).ToString());
    }

    public override void Write(Utf8JsonWriter writer, ITranslationsContainer? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(
            writer,
            new TranslationsContainer(value?.Version ?? "1.0"),
            options
        );
    }
}