using Microsoft.AspNetCore.Http; // Bắt buộc phải có để dùng IFormFile
using System;

namespace Server.DTOs
{
	public class NarrationDto
	{
		public int NarrationId { get; set; }
		public int PoiId { get; set; }
		public string PoiName { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public string LanguageCode { get; set; } = string.Empty;
		public string LanguageName { get; set; } = string.Empty;

		// Audio File (Cloudinary)
		public string? AudioUrl { get; set; }
		public string? AudioPublicId { get; set; }

		public int? DurationSeconds { get; set; }
		public bool UseAudioFile { get; set; }

		// TTS fallback
		public string? VoiceName { get; set; }
		public double SpeechRate { get; set; } = 0.5;
		public double Volume { get; set; } = 1.0;
	}

	public class CreateNarrationDto
	{
		public int PoiId { get; set; }
		public string Text { get; set; } = string.Empty;
		public string LanguageCode { get; set; } = string.Empty;

		// BỎ HẲN AudioUrl và AudioPublicId Ở ĐÂY VÌ MÌNH SẼ UPLOAD FILE SAU

		public int? DurationSeconds { get; set; }
		public bool UseAudioFile { get; set; } = false;

		// TTS fallback
		public string? VoiceName { get; set; } // Phải có dấu ? để cho phép rỗng
		public double SpeechRate { get; set; } = 0.5;
		public double Volume { get; set; } = 1.0;
	}

	public class UpdateNarrationDto
	{
		public string Text { get; set; } = string.Empty;

		// BỎ HẲN AudioUrl và AudioPublicId Ở ĐÂY VÌ NÓ SẼ KHÔNG BỊ GHI ĐÈ KHI UPDATE THÔNG TIN CHUNG

		public int? DurationSeconds { get; set; }
		public bool UseAudioFile { get; set; }

		// TTS
		public string? VoiceName { get; set; } // Phải có dấu ? để cho phép rỗng
		public double SpeechRate { get; set; } = 0.5;
		public double Volume { get; set; } = 1.0;
	}

	/// <summary>
	/// Dùng để upload file audio lên Cloudinary và gắn vào Narration
	/// </summary>
	public class NarrationAudioUploadDto
	{
		public int NarrationId { get; set; }
		public IFormFile File { get; set; } = default!; // Phải có = default! hoặc set null
		public int? DurationSeconds { get; set; }
	}
}