namespace VinhKhanh.WebAdmin.Models
{
	public class DashboardStatsDto
	{
		public int TotalPois { get; set; }
		public int TotalRestaurants { get; set; }
		public int TotalFoods { get; set; }
		public int TotalReviews { get; set; }
		public List<TopPoiDto> TopPois { get; set; } = new();
	}

	public class TopPoiDto
	{
		public int PoiId { get; set; }
		public string Name { get; set; } = "";
		public double AverageRating { get; set; }
		public int ReviewCount { get; set; }
	}
}