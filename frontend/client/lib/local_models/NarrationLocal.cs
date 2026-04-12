using SQLite;

namespace client.lib.model;

[Table("Narrations")]
public class NarrationLocal
{
    [PrimaryKey]
    public int NarrationId { get; set; } // Lấy đúng ID từ Server

    [Indexed]
    public int PoiId { get; set; } // Khóa ngoại trỏ về POIs

    public string LanguageCode { get; set; } // 'vi', 'en', 'ko'
    public string Text { get; set; }

    public bool UseAudioFile { get; set; }
    public string AudioUrl { get; set; }
}