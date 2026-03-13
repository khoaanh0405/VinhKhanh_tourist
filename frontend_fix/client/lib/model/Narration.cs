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

        // ===== AUDIO FILE (Cloudinary) =====

        [JsonPropertyName("audioUrl")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("audioPublicId")]
        public string? AudioPublicId { get; set; } // [Thêm mới] Đồng bộ với Backend

        [JsonPropertyName("durationSeconds")]
        public int? DurationSeconds { get; set; }

        [JsonPropertyName("useAudioFile")]
        public bool UseAudioFile { get; set; }

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