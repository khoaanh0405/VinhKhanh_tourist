using System.Collections.Generic;

namespace Server.DTOs
{
    public class RestaurantDto
    {
        public int RestaurantId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public List<FoodDto> Foods { get; set; }
    }

    public class FoodDto
    {
        public int FoodId { get; set; }
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public string Description { get; set; }
    }
}
