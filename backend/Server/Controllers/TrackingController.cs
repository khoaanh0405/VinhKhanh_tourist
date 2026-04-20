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

        [HttpPost("scan-qr")]
        public async Task<IActionResult> ScanQr([FromBody] ScanQrRequest request)
        {
            // Kiểm tra xem có gửi ít nhất 1 trong 2 mã không
            if (request.PoiId <= 0 && request.PlaylistId <= 0)
                return BadRequest("Phải có PoiId hoặc PlaylistId");

            // Logic chống spam (giữ nguyên cơ chế 3 phút của bạn)
            var timeThreshold = DateTime.UtcNow.AddSeconds(-5);
            var isSpam = await _context.QRScanLogs
                .AnyAsync(x => x.DeviceId == request.DeviceId
                            && x.PoiId == request.PoiId
                            && x.PlaylistId == request.PlaylistId // Thêm kiểm tra trùng playlist
                            && x.ScannedAt >= timeThreshold);

            if (isSpam) return Ok(new { message = "Spam detected" });

            var newLog = new QRScanLog
            {
                DeviceId = request.DeviceId,
                PoiId = request.PoiId > 0 ? request.PoiId : (int?)null,
                PlaylistId = request.PlaylistId > 0 ? request.PlaylistId : (int?)null,
                ScannedAt = DateTime.UtcNow
            };

            _context.QRScanLogs.Add(newLog);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Ghi nhận quét Playlist thành công" });
        }
    }
}