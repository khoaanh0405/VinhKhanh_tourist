using System.Text.Json.Serialization;

namespace VinhKhanh.WebAdmin.Models
{
    public class NarrationDto
    {
        [JsonPropertyName("narrationId")]
        public int NarrationId { get; set; }

        [JsonPropertyName("poiId")]
        public int PoiId { get; set; }

        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = "vi";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("voiceName")]
        public string? VoiceName { get; set; }

        [JsonPropertyName("speechRate")]
        public double SpeechRate { get; set; } = 1.0;

        [JsonPropertyName("volume")]
        public double Volume { get; set; } = 1.0;
    }

    public record CreateNarrationDto(int PoiId, string LanguageCode, string Text, string? VoiceName, double SpeechRate = 1.0, double Volume = 1.0);
    public record UpdateNarrationDto(string Text, string? VoiceName, double SpeechRate = 1.0, double Volume = 1.0);
}