using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Authorization
{
	public class ManagerRestaurantRequirement : IAuthorizationRequirement { }

	public class ManagerRestaurantHandler : AuthorizationHandler<ManagerRestaurantRequirement>
	{
		private readonly AppDbContext _db;
		private readonly IHttpContextAccessor _httpContext;

		public ManagerRestaurantHandler(AppDbContext db, IHttpContextAccessor httpContext)
		{
			_db = db;
			_httpContext = httpContext;
		}

		protected override async Task HandleRequirementAsync(
			AuthorizationHandlerContext context,
			ManagerRestaurantRequirement requirement)
		{
			// Admin thì có thẻ VIP, đi đâu cũng được pass hết
			if (context.User.IsInRole("Admin"))
			{
				context.Succeed(requirement);
				return;
			}

			var userIdStr = context.User.FindFirst("sub")?.Value;
			if (!int.TryParse(userIdStr, out var userId)) return;

			// Lấy ID của quán ăn từ đường dẫn URL (route)
			var httpCtx = _httpContext.HttpContext!;
			var routeRestaurantId = httpCtx.GetRouteValue("id")?.ToString()
								 ?? httpCtx.GetRouteValue("restaurantId")?.ToString();

			if (routeRestaurantId == null || !int.TryParse(routeRestaurantId, out var restId))
			{
				context.Fail();
				return;
			}

			// Kiểm tra xem User này có đúng là quản lý của Quán ăn này không
			var owns = await _db.Restaurants
				.AnyAsync(r => r.RestaurantId == restId && r.ManagerUserId == userId);

			if (owns) context.Succeed(requirement);
		}
	}
}