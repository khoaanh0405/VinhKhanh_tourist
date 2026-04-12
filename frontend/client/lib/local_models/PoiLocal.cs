using SQLite;

namespace client.lib.model;

[Table("POIs")]
public class PoiLocal
{
    [PrimaryKey]
    public int PoiId { get; set; } // Lấy đúng ID từ Server về

    public string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    // SQLite không lưu được List<string>. Ta sẽ gộp các link ảnh thành 1 chuỗi cách nhau bởi dấu phẩy
    public string ImageUrlsJoined { get; set; }

}