using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

[Route("api/[controller]")]
[ApiController]
public class FoodController : ControllerBase
{
    private readonly AppDbContext _context;

    public FoodController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Food/ByRestaurant/5 (Lấy menu của 1 quán)
    [HttpGet("ByRestaurant/{restaurantId}")]
    public async Task<ActionResult<IEnumerable<Food>>> GetFoodsByRestaurant(int restaurantId)
    {
        return await _context.Foods
                             .Where(f => f.RestaurantId == restaurantId)
                             .ToListAsync();
    }

    // POST: api/Food (Thêm món mới)
    [HttpPost]
    public async Task<ActionResult<Food>> PostFood(Food food)
    {
        var resExists = await _context.Restaurants.AnyAsync(r => r.RestaurantId == food.RestaurantId);
        if (!resExists) return BadRequest("Restaurant ID không tồn tại.");

        _context.Foods.Add(food);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFoodsByRestaurant), new { restaurantId = food.RestaurantId }, food);
    }

    // PUT: api/Food/5 (Cập nhật giá/tên món)
    [HttpPut("{id}")]
    public async Task<IActionResult> PutFood(int id, Food food)
    {
        if (id != food.FoodId) return BadRequest();

        _context.Entry(food).State = EntityState.Modified;
        food.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Food/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFood(int id)
    {
        var food = await _context.Foods.FindAsync(id);
        if (food == null) return NotFound();

        _context.Foods.Remove(food);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}