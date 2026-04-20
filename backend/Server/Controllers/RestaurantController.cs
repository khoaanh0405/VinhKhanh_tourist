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

        // ── GET /api/Restaurant  (Admin only) ─────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRestaurants()
            => Ok(await _service.GetAllAsync());

        // ── GET /api/Restaurant/my  (Manager only) ────────────────────────
        [HttpGet("my")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetMy()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _service.GetMyAsync(userId);
            return result == null
                ? Ok(new List<object>())
                : Ok(new List<object> { result });
        }

        // ── GET /api/Restaurant/{id}  (Admin | Manager) ───────────────────
        // [CHANGED] Bỏ field Description (đã xóa khỏi Restaurants)
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
                managerUserId = restaurant.ManagerUserId,
                poiId = restaurant.PoiId,
                latitude = poi != null ? poi.Latitude : 0,
                longitude = poi != null ? poi.Longitude : 0
            });
        }

        // ── POST /api/Restaurant/{id}/assign-manager  (Admin only) ────────
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

        // ── PUT /api/Restaurant/my  (Manager only) ────────────────────────
        [HttpPut("my")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateMy([FromBody] UpdateRestaurantRequest req)
        {
            var userId = GetCurrentUserId();
            var result = await _service.UpdateMyAsync(userId, req);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // ── POST /api/Restaurant  (Admin only) ────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRestaurant([FromBody] CreateRestaurantReq req)
        {
            // 1. Kiểm tra xem POI có tồn tại không
            var poiExists = await _context.POIs.AnyAsync(p => p.PoiId == req.PoiId);
            if (!poiExists)
            {
                return BadRequest(new { message = "POI không tồn tại. Vui lòng chọn POI hợp lệ." });
            }

            // 2. Tạo Quán ăn và liên kết với POI đã có
            var newRes = new Restaurant
            {
                Name = req.Name,
                Address = req.Address,
                PoiId = req.PoiId // Liên kết bằng ID
            };

            _context.Restaurants.Add(newRes);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tạo Quán ăn thành công!", RestaurantId = newRes.RestaurantId });
        }

        // ── PUT /api/Restaurant/{id}  (Admin only) ────────────────────────
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRestaurant(int id, [FromBody] UpdateRestaurantReq req)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound("Không tìm thấy Quán ăn.");

            // Kiểm tra PoiId mới có tồn tại không
            var poiExists = await _context.POIs.AnyAsync(p => p.PoiId == req.PoiId);
            if (!poiExists)
            {
                return BadRequest(new { message = "POI mới không tồn tại." });
            }

            // Chỉ cập nhật thông tin của Quán ăn
            restaurant.Name = req.Name;
            restaurant.Address = req.Address;
            restaurant.PoiId = req.PoiId; // Cho phép đổi quán sang địa điểm (POI) khác

            // Đã xóa phần cập nhật tọa độ POI ở đây vì Admin chỉ đang sửa Restaurant

            await _context.SaveChangesAsync();
            return NoContent();
        }
        // ── DELETE /api/Restaurant/{id}  (Admin only) ─────────────────────
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRestaurant(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound("Không tìm thấy Quán ăn.");

            // Xóa POI sẽ cascade xóa Restaurant (ON DELETE CASCADE)
            var poi = await _context.POIs.FindAsync(restaurant.PoiId);
            if (poi != null)
                _context.POIs.Remove(poi);
            else
                _context.Restaurants.Remove(restaurant);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── [REMOVED] PUT /{id}/toggle-lock ───────────────────────────────
        // Lý do: Cột IsLocked đã bị xóa khỏi bảng Restaurants trong schema mới.

        // ─────────────────────────────────────────────────────────────────
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdClaim, out var id) ? id : 0;
        }
    }
}