using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class UITranslation
    {
        [JsonPropertyName("translationId")]
        public int TranslationId { get; set; }

        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = string.Empty;

        [JsonPropertyName("resourceKey")]
        public string ResourceKey { get; set; } = string.Empty;

        [JsonPropertyName("resourceValue")]
        public string ResourceValue { get; set; } = string.Empty;
    }
}