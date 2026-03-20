using System.Text.Json.Serialization;

namespace BgCommon.Localization.Json.Models.v1;

public struct TranslationEntity
{
    public readonly string Name;
    public readonly string Value;

    [JsonConstructor]
    public TranslationEntity(string name, string value)
    {
        Name = name;
        Value = value;
    }
}