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
            // 1. Kiểm tra xem POI có tồn tại không
            if (qrCode.PoiId.HasValue)
            {
                var poiExists = await _context.POIs.AnyAsync(p => p.PoiId == qrCode.PoiId);
                if (!poiExists) return BadRequest("POI ID không tồn tại.");
            }

            // 2. Tạo nội dung mã QR tự động nếu người dùng chưa nhập
            // Ví dụ: https://vinhkhanhtourism.com/poi/{poiId}
            if (string.IsNullOrEmpty(qrCode.CodeValue) && qrCode.PoiId.HasValue)
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
    }
}