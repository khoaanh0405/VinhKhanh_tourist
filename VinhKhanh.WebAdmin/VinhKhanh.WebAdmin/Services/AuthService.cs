using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public interface IAuthService
	{
		Task<bool> LoginAsync(LoginRequest request);
	}

	public class AuthService : IAuthService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;
		private readonly AuthenticationStateProvider _authStateProvider;

		public AuthService(HttpClient http, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
		{
			_http = http;
			_localStorage = localStorage;
			_authStateProvider = authStateProvider;
		}

		public async Task<bool> LoginAsync(LoginRequest request)
		{
			try
			{
				// ⚠️ TÀI LƯU Ý: Sửa "api/user/login" thành đường dẫn API đăng nhập thật của Backend bạn
				var response = await _http.PostAsJsonAsync("api/user/login", request);

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
					if (result != null && !string.IsNullOrEmpty(result.Token))
					{
						await _localStorage.SetItemAsync("authToken", result.Token);
						((CustomAuthStateProvider)_authStateProvider).NotifyUserLogin(result.Token);
						return true;
					}
				}
				return false;
			}
			catch
			{
				return false; // Lỗi mạng hoặc server sập
			}
		}
	}
}