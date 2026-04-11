using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ReviewController : ControllerBase
	{
		private readonly AppDbContext _context;
		public ReviewController(AppDbContext context) => _context = context;

		// GET: api/Review?search=abc
		[HttpGet]
		public async Task<IActionResult> GetReviews([FromQuery] string? search, [FromQuery] int? poiId)
		{
			var query = _context.Reviews.Include(r => r.POI).AsQueryable();

			// 1. Lọc theo Quán ăn (POI) nếu có truyền PoiId
			if (poiId.HasValue && poiId > 0)
			{
				query = query.Where(r => r.PoiId == poiId.Value);
			}

			// 2. Lọc theo thanh tìm kiếm (nếu có)
			if (!string.IsNullOrEmpty(search))
			{
				query = query.Where(r => r.Comment.Contains(search) || r.UserName.Contains(search));
			}

			var result = await query
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new {
					r.ReviewId,
					r.UserName,
					r.Rating,
					r.Comment,
					r.IsHidden,
					r.CreatedAt,
					PoiName = r.POI != null ? r.POI.Name : "N/A"
				})
				.ToListAsync();

			return Ok(result);
		}

		// PUT: api/Review/hide/5
		[HttpPut("toggle-hide/{id}")]
		public async Task<IActionResult> ToggleHide(int id)
		{
			var review = await _context.Reviews.FindAsync(id);
			if (review == null) return NotFound();

			review.IsHidden = !review.IsHidden; // Đảo ngược trạng thái ẩn/hiện
			await _context.SaveChangesAsync();
			return Ok(new { isHidden = review.IsHidden });
		}

		// DELETE: api/Review/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteReview(int id)
		{
			var review = await _context.Reviews.FindAsync(id);
			if (review == null) return NotFound();

			_context.Reviews.Remove(review);
			await _context.SaveChangesAsync();
			return NoContent();
		}
	}
}