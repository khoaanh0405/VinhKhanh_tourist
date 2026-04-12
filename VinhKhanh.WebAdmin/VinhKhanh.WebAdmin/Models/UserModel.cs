using System;

namespace VinhKhanh.WebAdmin.Models
{
	// 1. Khuôn để hứng dữ liệu danh sách người dùng từ Backend trả về
	public class UserModel
	{
		public int UserId { get; set; }
		public string DisplayName { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
		public string Email { get; set; }
		public DateTime CreatedAt { get; set; }
		public bool IsLocked { get; set; }
	}
	// 2. Khuôn để gửi dữ liệu từ Form tạo người dùng mới lên Backend
	public class RegisterRequest
	{
		public string DisplayName { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string Email { get; set; }
		public string Role { get; set; } = "Manager"; // Mặc định để sẵn là Manager cho tiện
	}

	// ĐÃ SỬA: Đưa class này vào BÊN TRONG namespace
	public class ChangePasswordRequest
	{
		public string NewPassword { get; set; } = string.Empty;
	}
}