using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

[Route("api/[controller]")]
[ApiController]
public class GeofenceController : ControllerBase
{
    private readonly AppDbContext _context;

    public GeofenceController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Geofence (Lấy danh sách để vẽ lên bản đồ Admin)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Geofence>>> GetGeofences()
    {
        return await _context.Geofences.Include(g => g.POI).ToListAsync();
    }

    // POST: api/Geofence
    [HttpPost]
    public async Task<ActionResult<Geofence>> PostGeofence(Geofence geofence)
    {
        // Mỗi POI chỉ nên có 1 Geofence chính? Nếu đúng thì check ở đây
        if (await _context.Geofences.AnyAsync(g => g.PoiId == geofence.PoiId))
        {
            return BadRequest("POI này đã có Geofence rồi.");
        }

        _context.Geofences.Add(geofence);
        await _context.SaveChangesAsync();
        return Ok(geofence);
    }

    // PUT: api/Geofence/5 (Điều chỉnh bán kính Radius)
    [HttpPut("{id}")]
    public async Task<IActionResult> PutGeofence(int id, Geofence geofence)
    {
        if (id != geofence.GeofenceId) return BadRequest();
        _context.Entry(geofence).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}