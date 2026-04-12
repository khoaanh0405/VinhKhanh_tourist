using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VinhKhanh.WebAdmin;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using VinhKhanh.WebAdmin.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. CẤU HÌNH ĐỊA CHỈ BACKEND (⚠️ Sửa cổng 7284 thành cổng Swagger Backend của bạn)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(" https://sln71gls-7284.asse.devtunnels.ms") });

// 2. ĐĂNG KÝ BẢO MẬT & LOCALSTORAGE
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<VinhKhanh.WebAdmin.Services.IUserService, VinhKhanh.WebAdmin.Services.UserService>();
builder.Services.AddScoped<VinhKhanh.WebAdmin.Services.IPoiService, VinhKhanh.WebAdmin.Services.PoiService>();
builder.Services.AddScoped<INarrationService, NarrationService>();
builder.Services.AddScoped<IFoodTranslationService, FoodTranslationService>();
builder.Services.AddScoped<IPoiTranslationService, PoiTranslationService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
//builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<VinhKhanh.WebAdmin.Services.DashboardService>();
// 3. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
builder.Services.AddScoped<IAuthService, AuthService>();
// Tí nữa mình sẽ đăng ký IRestaurantService và IFoodService ở ngay dưới dòng này

await builder.Build().RunAsync();