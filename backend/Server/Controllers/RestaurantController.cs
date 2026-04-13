using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class RestaurantController : ControllerBase
	{
		private readonly IRestaurantService _service;
		private readonly AppDbContext _context;

		public RestaurantController(IRestaurantService service, AppDbContext context)
		{
			_service = service;
			_context = context;
		}

		// ======================================================
		// --- CÁC HÀM GET DỮ LIỆU ---
		// ======================================================

		[HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> GetRestaurants()
		{
			var results = await _service.GetAllAsync();
			return Ok(results);
		}

		// 👇 Đã đưa hàm "my" lên trên hàm "{id}" để tránh lỗi 404 Not Found 👇
		[HttpGet("my")]
		[Authorize(Roles = "Manager")]
		public async Task<IActionResult> GetMy()
		{
			var userId = GetCurrentUserId();
			if (userId == 0) return Unauthorized(new { message = "Không xác định được người dùng." });

			// Backend vẫn tìm ra 1 quán
			var result = await _service.GetMyAsync(userId);

			if (result == null)
			{
				// Thay vì báo lỗi 404, trả về một MẢNG RỖNG để Frontend không bị lỗi
				return Ok(new List<object>());
			}

			// 👇 CHỖ QUAN TRỌNG NHẤT 👇
			// Gói cái kết quả (Object) vào trong một cái Danh sách (List) 
			// để Frontend đọc được bằng vòng lặp foreach
			return Ok(new List<object> { result });
		}

		// Thêm :int vào {id} để hệ thống phân biệt được đâu là chữ "my", đâu là ID số
		[HttpGet("{id:int}")]
		[Authorize(Roles = "Admin, Manager")]
		public async Task<IActionResult> GetRestaurantById(int id)
		{
			var restaurant = await _context.Restaurants.FindAsync(id);
			if (restaurant == null) return NotFound(new { message = "Không tìm thấy quán ăn" });

			var poi = await _context.POIs.FindAsync(restaurant.PoiId);

			return Ok(new
			{
				restaurantId = restaurant.RestaurantId,
				name = restaurant.Name,
				address = restaurant.Address,
				description = restaurant.Description,
				managerUserId = restaurant.ManagerUserId,
				poiId = restaurant.PoiId,
				latitude = poi != null ? poi.Latitude : 0,
				longitude = poi != null ? poi.Longitude : 0
			});
		}

		// ======================================================
		// --- CÁC HÀM GÁN QUẢN LÝ VÀ DÀNH CHO MANAGER ---
		// ======================================================

		[HttpPost("{id}/assign-manager")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AssignManager(int id, [FromBody] AssignManagerRequest req)
		{
			var restaurant = await _context.Restaurants.FindAsync(id);
			if (restaurant == null) return NotFound(new { message = "Không tìm thấy quán ăn." });

			if (req.ManagerUserId <= 0)
			{
				restaurant.ManagerUserId = null;
			}
			else
			{
				bool isAlreadyManaging = await _context.Restaurants
					.AnyAsync(r => r.ManagerUserId == req.ManagerUserId && r.RestaurantId != id);

				if (isAlreadyManaging)
					return BadRequest(new { message = "Manager này đã quản lý một quán khác rồi." });

				restaurant.ManagerUserId = req.ManagerUserId;
			}

			await _context.SaveChangesAsync();
			return NoContent();
		}

		[HttpPut("my")]
		[Authorize(Roles = "Manager")]
		public async Task<IActionResult> UpdateMy([FromBody] UpdateRestaurantRequest req)
		{
			var userId = GetCurrentUserId();
			var result = await _service.UpdateMyAsync(userId, req);
			if (result == null) return NotFound();
			return Ok(result);
		}

		// 👇 Đã gộp thành 1 hàm GetCurrentUserId duy nhất, không còn lỗi trùng lặp 👇
		private int GetCurrentUserId()
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
							 ?? User.FindFirst("sub")?.Value;

			return int.TryParse(userIdClaim, out var id) ? id : 0;
		}

		// ======================================================
		// --- CÁC HÀM MỚI CHO ADMIN (GỘP TẠO POI + QUÁN ĂN) ---
		// ======================================================

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> CreateRestaurant([FromBody] CreateRestaurantReq req)
		{
			var newPoi = new POI
			{
				Name = req.Name,
				Latitude = req.Latitude,
				Longitude = req.Longitude,
				Description = req.Description,
				AverageRating = 0,
				ReviewCount = 0
			};

			_context.POIs.Add(newPoi);
			await _context.SaveChangesAsync();

			var newRes = new Restaurant
			{
				Name = req.Name,
				Address = req.Address,
				Description = req.Description,
				PoiId = newPoi.PoiId
			};

			_context.Restaurants.Add(newRes);
			await _context.SaveChangesAsync();

			return Ok(new { Message = "Tạo Quán ăn và POI thành công!" });
		}

		[HttpPut("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> UpdateRestaurant(int id, [FromBody] UpdateRestaurantReq req)
		{
			var restaurant = await _context.Restaurants.FindAsync(id);
			if (restaurant == null) return NotFound("Không tìm thấy Quán ăn.");

			restaurant.Name = req.Name;
			restaurant.Address = req.Address;
			restaurant.Description = req.Description;

			var poi = await _context.POIs.FindAsync(req.PoiId);
			if (poi != null)
			{
				poi.Name = req.Name;
				poi.Latitude = req.Latitude;
				poi.Longitude = req.Longitude;
				poi.Description = req.Description;
			}

			await _context.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeleteRestaurant(int id)
		{
			var restaurant = await _context.Restaurants.FindAsync(id);
			if (restaurant == null) return NotFound("Không tìm thấy Quán ăn.");

			var poi = await _context.POIs.FindAsync(restaurant.PoiId);

			if (poi != null)
			{
				_context.POIs.Remove(poi);
			}
			else
			{
				_context.Restaurants.Remove(restaurant);
			}

			await _context.SaveChangesAsync();
			return NoContent();
		}

		[HttpPut("{id}/toggle-lock")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> ToggleLock(int id)
		{
			var restaurant = await _context.Restaurants.FindAsync(id);
			if (restaurant == null) return NotFound(new { message = "Không tìm thấy quán ăn" });

			restaurant.IsLocked = !restaurant.IsLocked;
			await _context.SaveChangesAsync();

			return Ok(new { isLocked = restaurant.IsLocked });
		}
	}
}