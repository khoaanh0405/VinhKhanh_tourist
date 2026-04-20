using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using static Server.DTOs.DashboardStatsDto;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, Manager")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context) => _context = context;
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            // Quy ước: Những máy bắn Heartbeat trong vòng 60 giây đổ lại thì được xem là ĐANG ONLINE
            var activeThreshold = DateTime.UtcNow.AddSeconds(-10);

            var stats = new DashboardStatsDto
            {
                // --- 3 CHỈ SỐ MỚI ---
                TotalVisits = await _context.Devices.CountAsync(),
                ActiveUsers = await _context.ActiveSessions.CountAsync(x => x.LastSeenAt >= activeThreshold),
                TotalQrScans = await _context.QRScanLogs.CountAsync(),

                // --- CÁC CHỈ SỐ CŨ CỦA BẠN ---
                TotalPois = await _context.POIs.CountAsync(),
                TotalRestaurants = await _context.Restaurants.CountAsync(),
                TotalFoods = await _context.Foods.CountAsync(),

                PoiScanStats = await _context.QRScanLogs
                    .GroupBy(x => x.PoiId)
                    .Select(g => new PoiScanStatDto
                    {
                        PoiName = _context.POIs.Where(p => p.PoiId == g.Key).Select(p => p.Name).FirstOrDefault() ?? "Không rõ",
                        ScanCount = g.Count()
                    })
                    .OrderByDescending(x => x.ScanCount)
                    .ToListAsync()
            };

            var poiStats = await _context.QRScanLogs
                .GroupBy(x => x.PoiId)
                .Select(g => new PoiScanStatDto
                {
                    // Lấy tên quán dựa trên PoiId
                    PoiName = _context.POIs.Where(p => p.PoiId == g.Key).Select(p => p.Name).FirstOrDefault() ?? "Không rõ",
                    ScanCount = g.Count()
                })
                .OrderByDescending(x => x.ScanCount)
                .ToListAsync();

                        stats.PoiScanStats = poiStats;

            return Ok(stats);
        }
    }
}