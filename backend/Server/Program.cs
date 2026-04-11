using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Authorization; // Để nhận diện Policies và Handler
using Server.Data;
using Server.Models;
using Server.Services; // Đảm bảo namespace này chứa ICloudinaryService và CloudinaryService
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

// ============================
// 1. DATABASE
// ============================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// ============================
// 2. JWT AUTH
// ============================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyStr = jwtSettings["Key"];
var key = Encoding.UTF8.GetBytes(
    keyStr ?? "KeyMacDinhPhaiDaiHon16KyTuDeKhongBiLoi");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// ============================
// 3. CONTROLLERS + SWAGGER
// ============================
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
		options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vinh Khanh Tourism API",
        Version = "v1",
        Description = "Multi-language narration API (Cloudinary Enabled)" // Đã đổi mô tả
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token theo định dạng: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ============================
// 4. CORS
// ============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================
// 5. REGISTER SERVICES
// ============================
builder.Services.AddHttpContextAccessor(); // Cực kỳ quan trọng để kiểm tra quyền sở hữu
builder.Services.AddScoped<INarrationService, NarrationService>();

// --- THÊM MẤY DÒNG NÀY VÀO ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IFoodService, FoodService>();

// Đăng ký "Anh bảo vệ" kiểm tra chủ quán
builder.Services.AddScoped<IAuthorizationHandler, ManagerRestaurantHandler>();

// ============================
// 6. CLOUDINARY STORAGE
// ============================
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();


// ============================
// 8. AUTHORIZATION POLICIES
// ============================
builder.Services.AddAuthorization(options =>
{
	// Quyền chỉ dành cho Admin
	options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole("Admin"));

	// Quyền cho Admin hoặc Manager
	options.AddPolicy(Policies.ManagerOrAdmin, policy => policy.RequireRole("Admin", "Manager"));

	// Quyền đặc biệt: Phải là Admin HOẶC là chủ của chính quán ăn đó
	options.AddPolicy(Policies.OwnsRestaurant, policy =>
		policy.Requirements.Add(new ManagerRestaurantRequirement()));
});

// ============================
// 7. LOGGING
// ============================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();


// ============================
// PIPELINE
// ============================

// 1. Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json",
            "Vinh Khanh Tourism API v1");
        c.RoutePrefix = string.Empty;
    });
}

// 2. CORS
app.UseCors("AllowAll");

// 3. Static Files (nếu vẫn dùng wwwroot cho file local)
app.UseStaticFiles();

// 4. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 5. Map Controllers
app.MapControllers();

// ============================
// AUTO MIGRATE DATABASE
// ============================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate(); // Khuyến nghị dùng Migrate thay vì EnsureCreated
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi khởi tạo Database.");
    }
}

app.Run();