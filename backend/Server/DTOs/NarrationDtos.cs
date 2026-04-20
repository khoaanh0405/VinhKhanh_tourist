namespace Server.DTOs
{
    public class NarrationDto
    {
        public int NarrationId { get; set; }
        public int PoiId { get; set; }
        public string PoiName { get; set; } = string.Empty;

        public string LanguageCode { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        // TTS settings
        public string? VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;

        public DateTime CreatedAt { get; set; }
    }

    public class CreateNarrationDto
    {
        public int PoiId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        // TTS settings
        public string? VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;
    }

    public class UpdateNarrationDto
    {
        public string Text { get; set; } = string.Empty;

        // TTS settings
        public string? VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;
    }
}