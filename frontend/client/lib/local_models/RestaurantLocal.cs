using SQLite;

namespace client.lib.model;

[Table("Restaurants")]
public class RestaurantLocal
{
    [PrimaryKey]
    public int RestaurantId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string Description { get; set; }

    [Indexed] // Đánh index để truy vấn nhanh hơn
    public int PoiId { get; set; } // Khóa ngoại trỏ về POI
}