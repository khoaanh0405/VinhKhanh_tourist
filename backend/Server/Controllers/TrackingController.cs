using Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrackingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return BadRequest("DeviceId is required");

            // Bước 1: Kiểm tra xem thiết bị này đã từng truy cập chưa (Total Visits)
            var device = await _context.Devices.FindAsync(request.DeviceId);
            if (device == null)
            {
                // Nếu chưa có -> Tạo mới
                device = new Device { DeviceId = request.DeviceId, FirstSeenAt = DateTime.UtcNow };
                _context.Devices.Add(device);
            }

            // Bước 2: Cập nhật thời gian Online gần nhất (Active Users)
            var session = await _context.ActiveSessions.FindAsync(request.DeviceId);
            if (session == null)
            {
                _context.ActiveSessions.Add(new ActiveSession { DeviceId = request.DeviceId, LastSeenAt = DateTime.UtcNow });
            }
            else
            {
                session.LastSeenAt = DateTime.UtcNow; // Cập nhật lại thời gian ping mới nhất
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Heartbeat recorded" });
        }

        // 2. API: Ghi nhận lượt quét mã QR
        [HttpPost("scan-qr")]
        public async Task<IActionResult> ScanQr([FromBody] ScanQrRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId) || request.PoiId <= 0)
                return BadRequest("Invalid data");

            // Bắt buộc thiết bị phải tồn tại trong bảng Devices trước khi lưu Log
            var deviceExists = await _context.Devices.AnyAsync(d => d.DeviceId == request.DeviceId);
            if (!deviceExists)
            {
                _context.Devices.Add(new Device { DeviceId = request.DeviceId, FirstSeenAt = DateTime.UtcNow });
                await _context.SaveChangesAsync();
            }

            // LOGIC CHỐNG TRÙNG LẶP (Chống Spam): 
            // Nếu cùng 1 máy, quét cùng 1 mã POI trong vòng 3 phút đổ lại -> BỎ QUA không cộng dồn
            var timeThreshold = DateTime.UtcNow.AddMinutes(-3);

            var isSpam = await _context.QRScanLogs
                .AnyAsync(x => x.DeviceId == request.DeviceId
                            && x.PoiId == request.PoiId
                            && x.ScannedAt >= timeThreshold);

            if (isSpam)
            {
                // Vẫn trả về 200 OK để App Mobile chạy tiếp bình thường, nhưng Backend không tăng số lượng đếm
                return Ok(new { message = "Scan ignored (Duplicate within 3 minutes)" });
            }

            // Ghi nhận lượt quét hợp lệ
            var newLog = new QRScanLog
            {
                DeviceId = request.DeviceId,
                PoiId = request.PoiId,
                ScannedAt = DateTime.UtcNow
            };

            _context.QRScanLogs.Add(newLog);
            await _context.SaveChangesAsync();

            return Ok(new { message = "QR Scan recorded successfully" });
        }
    }
}