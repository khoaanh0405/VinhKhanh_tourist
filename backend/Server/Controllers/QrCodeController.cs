using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QRCodeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QRCodeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/QRCode
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QRCode>>> GetQRCodes()
        {
            // Kèm thông tin POI để biết mã này của quán nào
            return await _context.QRCodes.Include(q => q.POI).ToListAsync();
        }

        // GET: api/QRCode/5
        [HttpGet("{id}")]
        public async Task<ActionResult<QRCode>> GetQRCode(int id)
        {
            var qrCode = await _context.QRCodes.Include(q => q.POI)
                                               .FirstOrDefaultAsync(q => q.QRCodeId == id);

            if (qrCode == null) return NotFound("Mã QR không tồn tại.");

            return qrCode;
        }

        // POST: api/QRCode
        [HttpPost]
        public async Task<ActionResult<QRCode>> PostQRCode(QRCode qrCode)
        {
            // Thay vì qrCode.PoiId.HasValue
            if (qrCode.PoiId > 0)
            {
                var poiExists = await _context.POIs.AnyAsync(p => p.PoiId == qrCode.PoiId);
                if (!poiExists) return BadRequest("POI ID không tồn tại.");
            }

            // Thay đổi điều kiện tạo nội dung tự động tương ứng
            if (string.IsNullOrEmpty(qrCode.CodeValue) && qrCode.PoiId > 0)
            {
                qrCode.CodeValue = $"https://myapp.com/poi/{qrCode.PoiId}";
            }

            _context.QRCodes.Add(qrCode);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetQRCode", new { id = qrCode.QRCodeId }, qrCode);
        }

        // DELETE: api/QRCode/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQRCode(int id)
        {
            var qrCode = await _context.QRCodes.FindAsync(id);
            if (qrCode == null) return NotFound();

            _context.QRCodes.Remove(qrCode);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // 1. Lấy mã QR trực tiếp bằng mã Quán (PoiId) thay vì QRCodeId
        [HttpGet("poi/{poiId}")]
        public async Task<IActionResult> GetByPoiId(int poiId)
        {
            var qr = await _context.QRCodes.FirstOrDefaultAsync(q => q.PoiId == poiId);
            if (qr == null) return NotFound(new { message = "Chưa có mã QR" });

            return Ok(qr);
        }

        // 2. Nút bấm "Tạo nhanh QR" từ giao diện
        [Authorize]
        [HttpPost("generate/{poiId}")]
        public async Task<IActionResult> GenerateForPoi(int poiId)
        {
            try
            {
                var existingQr = await _context.QRCodes.FirstOrDefaultAsync(q => q.PoiId == poiId);
                if (existingQr != null) return Ok(existingQr);

                string uniqueCode = $"app://vinhkhanh/poi/{poiId}";

                var newQr = new QRCode
                {
                    PoiId = poiId,
                    CodeValue = uniqueCode
                };

                _context.QRCodes.Add(newQr);
                await _context.SaveChangesAsync();

                return Ok(newQr);
            }
            catch (Exception ex)
            {
                // 👇 Dòng này sẽ in cái lỗi "thật sự" ra màn hình đen (Terminal) của bạn 👇
                Console.WriteLine("LỖI LƯU QR: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("CHI TIẾT: " + ex.InnerException.Message);

                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("playlist/{playlistId}")]
        public async Task<IActionResult> GetByPlaylistId(int playlistId)
        {
            var qr = await _context.QRCodes
                .FirstOrDefaultAsync(q => q.PlaylistId == playlistId);

            if (qr == null)
            {
                // Trả về HTTP 200 OK với object rỗng thay vì NotFound (404)
                // Điều này giúp trình duyệt không báo lỗi đỏ, Frontend Deserialize vào class QRCodeDto 
                // sẽ tự động nhận CodeValue = "" và QRCodeId = 0.
                return Ok(new { QRCodeId = 0, CodeValue = "" });
            }

            return Ok(qr);
        }

        // POST: api/QRCode/generate-playlist/{playlistId}  -- Tự động tạo QR cho Playlist
        [Authorize]
        [HttpPost("generate-playlist/{playlistId}")]
        public async Task<IActionResult> GenerateForPlaylist(int playlistId)
        {
            try
            {
                // Nếu đã có QR rồi thì trả luôn, không tạo thêm
                var existing = await _context.QRCodes
                    .FirstOrDefaultAsync(q => q.PlaylistId == playlistId);
                if (existing != null) return Ok(existing);

                var playlistExists = await _context.Playlists
                    .AnyAsync(p => p.PlaylistId == playlistId);
                if (!playlistExists)
                    return NotFound(new { message = "Playlist không tồn tại." });

                var newQr = new QRCode
                {
                    PoiId = null,                                            // KHÔNG gắn POI
                    PlaylistId = playlistId,
                    CodeValue = $"app://vinhkhanh/playlist/{playlistId}"    // Deep-link chuẩn
                };

                _context.QRCodes.Add(newQr);
                await _context.SaveChangesAsync();
                return Ok(newQr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("LỖI SINH QR PLAYLIST: " + ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}