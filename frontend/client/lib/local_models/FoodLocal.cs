using SQLite;

namespace client.lib.model;

[Table("Foods")]
public class FoodLocal
{
    [PrimaryKey]
    public int FoodId { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
    public string Description { get; set; }

    [Indexed]
    public int RestaurantId { get; set; } // Khóa ngoại trỏ về Restaurant
}