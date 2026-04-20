using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistController : ControllerBase
    {
        private readonly IPlaylistService _service;

        public PlaylistController(IPlaylistService service)
            => _service = service;

        // ── GET /api/Playlist ─────────────────────────────────────────
        // Trả về danh sách tất cả Playlist (Admin dùng để quản lý)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        // ── GET /api/Playlist/{id}/items ──────────────────────────────
        // PUBLIC endpoint: Mobile quét QR xong gọi ngay endpoint này.
        // Không cần đăng nhập vì đây là dữ liệu công khai.
        [HttpGet("{id:int}/items")]
        [AllowAnonymous]
        public async Task<IActionResult> GetItems(int id)
        {
            var items = await _service.GetItemsAsync(id);
            if (!items.Any())
                return NotFound(new { message = $"Playlist #{id} không tồn tại hoặc không có địa điểm nào." });

            return Ok(items);
        }

        // ── POST /api/Playlist ────────────────────────────────────────
        // Admin tạo Playlist mới từ giao diện Web Admin.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePlaylistDto dto)
        {
            try
            {
                var playlist = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetItems),
                    new { id = playlist.PlaylistId },
                    new { playlist.PlaylistId, playlist.Title });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreatePlaylistDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Title) || dto.PoiIds == null || !dto.PoiIds.Any())
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ. Cần có tên Playlist và ít nhất 1 địa điểm." });
                }

                var success = await _service.UpdateAsync(id, dto);

                if (!success)
                    return NotFound(new { message = "Playlist không tồn tại hoặc đã bị xóa." });

                return Ok(); // Trả về 200 OK
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // ── DELETE /api/Playlist/{id} ─────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound(new { message = "Playlist không tồn tại." });
        }
    }
}