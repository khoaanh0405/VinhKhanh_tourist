using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Server.Services;
using Server.DTOs;
using System.Security.Claims;
using Server.Models;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FoodController : ControllerBase
    {
        private readonly IFoodService _service;

        public FoodController(IFoodService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("my")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetMyFoods()
        {
            var userId = GetCurrentUserId();
            var result = await _service.GetMyAsync(userId);
            return Ok(result);
        }

        // 👇 DÙNG KHUÔN "FoodSubmitRequest" TỪ FILE DTO CỦA BẠN
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateFood([FromBody] FoodSubmitRequest req)
        {
            var isAdmin = User.IsInRole("Admin");
            if (isAdmin)
            {
                var newFood = new Food
                {
                    Name = req.Name,
                    Price = req.Price,
                    Description = req.Description,
                    RestaurantId = req.RestaurantId
                };
                var success = await _service.CreateAdminAsync(newFood);
                if (success) return Ok();
                return BadRequest("Lỗi khi thêm món.");
            }
            else
            {
                var userId = GetCurrentUserId();
                var createReq = new CreateFoodRequest(req.Name, req.Price, req.Description);
                var result = await _service.CreateMyAsync(userId, createReq);
                if (result == null) return BadRequest(new { message = "Bạn không quản lý quán ăn nào." });
                return Ok(result);
            }
        }

        // 👇 TƯƠNG TỰ CHO HÀM SỬA
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateFood(int id, [FromBody] FoodSubmitRequest req)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var updateReq = new UpdateFoodRequest(req.Name, req.Price, req.Description);

            var success = await _service.UpdateAsync(id, userId, updateReq, isAdmin);
            if (!success) return Forbid();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteFood(int id)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var success = await _service.DeleteAsync(id, userId, isAdmin);
            if (!success) return Forbid();
            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}   