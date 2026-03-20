using System.Text.Json.Serialization;

namespace BgCommon.Localization.Json.Models.v1;

public sealed record TranslationFile : TranslationsContainer
{
    public readonly IEnumerable<TranslationEntity> Strings;

    [JsonConstructor]
    public TranslationFile(string version, IEnumerable<TranslationEntity> strings)
        : base(version)
    {
        Version = version;
        Strings = strings;
    }
}