using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
// using Server.Data; // Thay bằng namespace chứa DbContext của bạn

[Route("api/[controller]")]
[ApiController]
public class RestaurantController : ControllerBase
{
    private readonly AppDbContext _context;

    public RestaurantController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Restaurant
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurants()
    {
        return await _context.Restaurants.Include(r => r.POI).ToListAsync();
    }

    // GET: api/Restaurant/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Restaurant>> GetRestaurant(int id)
    {
        var restaurant = await _context.Restaurants
                                       .Include(r => r.Foods) // Lấy luôn menu
                                       .FirstOrDefaultAsync(r => r.RestaurantId == id);

        if (restaurant == null) return NotFound();
        return restaurant;
    }

    // POST: api/Restaurant (Thêm quán mới)
    [HttpPost]
    public async Task<ActionResult<Restaurant>> PostRestaurant(Restaurant restaurant)
    {
        // Kiểm tra POI có tồn tại không
        var poiExists = await _context.POIs.AnyAsync(p => p.PoiId == restaurant.PoiId);
        if (!poiExists) return BadRequest("POI ID không tồn tại.");

        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetRestaurant", new { id = restaurant.RestaurantId }, restaurant);
    }

    // PUT: api/Restaurant/5 (Sửa thông tin quán)
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRestaurant(int id, Restaurant restaurant)
    {
        if (id != restaurant.RestaurantId) return BadRequest();

        _context.Entry(restaurant).State = EntityState.Modified;
        restaurant.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Restaurants.Any(e => e.RestaurantId == id)) return NotFound();
            else throw;
        }

        return NoContent();
    }

    // DELETE: api/Restaurant/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRestaurant(int id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null) return NotFound();

        _context.Restaurants.Remove(restaurant);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}