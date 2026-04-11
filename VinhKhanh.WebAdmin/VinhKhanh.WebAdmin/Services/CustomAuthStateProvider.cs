using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace VinhKhanh.WebAdmin.Services
{
	public class CustomAuthStateProvider : AuthenticationStateProvider
	{
		private readonly ILocalStorageService _localStorage;
		private readonly HttpClient _http;

		public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
		{
			_localStorage = localStorage;
			_http = http;
		}

		public override async Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");

			if (string.IsNullOrWhiteSpace(token))
				return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

			_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var claims = ParseClaimsFromJwt(token);
			var identity = new ClaimsIdentity(claims, "jwt");
			return new AuthenticationState(new ClaimsPrincipal(identity));
		}

		public void NotifyUserLogin(string token)
		{
			var claims = ParseClaimsFromJwt(token);
			var identity = new ClaimsIdentity(claims, "jwt");
			NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
		}

		public void NotifyUserLogout()
		{
			NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
		}

		// Hàm "thần thánh" dịch mã JWT và ép chuẩn Role của Microsoft
		private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
		{
			var claims = new List<Claim>();
			var payload = jwt.Split('.')[1];
			var jsonBytes = ParseBase64WithoutPadding(payload);
			var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

			if (keyValuePairs != null)
			{
				keyValuePairs.TryGetValue("role", out object? roles);
				if (roles != null)
				{
					if (roles.ToString()!.Trim().StartsWith("["))
					{
						var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString()!);
						foreach (var parsedRole in parsedRoles!)
							claims.Add(new Claim(ClaimTypes.Role, parsedRole));
					}
					else
					{
						claims.Add(new Claim(ClaimTypes.Role, roles.ToString()!));
					}
					keyValuePairs.Remove("role");
				}

				foreach (var kvp in keyValuePairs)
					claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
			}
			return claims;
		}

		private byte[] ParseBase64WithoutPadding(string base64)
		{
			switch (base64.Length % 4)
			{
				case 2: base64 += "=="; break;
				case 3: base64 += "="; break;
			}
			return Convert.FromBase64String(base64);
		}
	}
}