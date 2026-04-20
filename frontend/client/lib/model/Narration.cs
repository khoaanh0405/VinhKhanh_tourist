using System;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class Narration
    {
        [JsonPropertyName("narrationId")]
        public int NarrationId { get; set; }

        [JsonPropertyName("poiId")]
        public int PoiId { get; set; }

        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        // ===== TTS CONFIG =====
        [JsonPropertyName("voiceName")]
        public string? VoiceName { get; set; }

        [JsonPropertyName("speechRate")]
        public double SpeechRate { get; set; }

        [JsonPropertyName("volume")]
        public double Volume { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}