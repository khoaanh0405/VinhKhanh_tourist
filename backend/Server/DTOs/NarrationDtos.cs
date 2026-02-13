using System;

namespace Server.DTOs
{
    public class NarrationDto
    {
        public int NarrationId { get; set; }
        public int PoiId { get; set; }
        public string PoiName { get; set; }

        public string Text { get; set; }

        public string LanguageCode { get; set; }
        public string LanguageName { get; set; }

        // Cấu hình TTS cho Flutter
        public string VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;
    }

    public class CreateNarrationDto
    {
        public int PoiId { get; set; }
        public string Text { get; set; }
        public string LanguageCode { get; set; }

        // Cấu hình đọc
        public string VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;
    }

    public class UpdateNarrationDto
    {
        public string Text { get; set; }

        public string VoiceName { get; set; }
        public double SpeechRate { get; set; }
        public double Volume { get; set; }
    }
}
