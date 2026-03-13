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

        // Audio File (Cloudinary)
        public string? AudioUrl { get; set; }
        public string AudioPublicId { get; set; } // [Thêm mới]

        public int? DurationSeconds { get; set; }
        public bool UseAudioFile { get; set; }

        // TTS fallback
        public string VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;
    }

    public class CreateNarrationDto
    {
        public int PoiId { get; set; }
        public string Text { get; set; }
        public string LanguageCode { get; set; }

        // Audio (để trống nếu chưa có, upload sau)
        public string? AudioUrl { get; set; }
        public string AudioPublicId { get; set; } // [Thêm mới]

        public int? DurationSeconds { get; set; }
        public bool UseAudioFile { get; set; } = false;

        // TTS fallback
        public string VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;
    }

    public class UpdateNarrationDto
    {
        public string Text { get; set; }

        // Audio
        public string? AudioUrl { get; set; }
        public string AudioPublicId { get; set; } // [Thêm mới]

        public int? DurationSeconds { get; set; }
        public bool UseAudioFile { get; set; }

        // TTS
        public string VoiceName { get; set; }
        public double SpeechRate { get; set; }
        public double Volume { get; set; }
    }

    /// <summary>
    /// Dùng để upload file audio lên Cloudinary và gắn vào Narration
    /// </summary>
    public class NarrationAudioUploadDto
    {
        public int NarrationId { get; set; }
        public IFormFile File { get; set; }
        public int? DurationSeconds { get; set; }
    }
}