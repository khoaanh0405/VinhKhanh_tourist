namespace VinhKhanh.WebAdmin.Models
{
	// Đây là khuôn để nhận dữ liệu từ API về
	public class POIDto
	{
		public int PoiId { get; set; }
		public string Name { get; set; } = "";
		public double Latitude { get; set; }
		public double Longitude { get; set; }
	}

	// Đây là khuôn để gửi dữ liệu đi khi Thêm/Sửa
	public record CreatePoiAdminRequest(string Name, double Latitude, double Longitude);
	public record UpdatePoiAdminRequest(string Name, double Latitude, double Longitude);
}