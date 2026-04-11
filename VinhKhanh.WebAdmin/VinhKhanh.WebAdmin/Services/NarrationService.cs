using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Forms; // Để dùng IBrowserFile
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public interface INarrationService
	{
		Task<List<NarrationDto>> GetByPoiIdAsync(int poiId);
		Task<(bool Success, string Message)> CreateAsync(CreateNarrationDto req);
		Task<(bool Success, string Message)> UpdateAsync(int id, UpdateNarrationDto req);
		Task<bool> DeleteAsync(int id);
		Task<bool> UploadAudioAsync(int narrationId, IBrowserFile file); // 👈 Hàm Upload Audio mới
	}

	public class NarrationService : INarrationService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		public NarrationService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http;
			_localStorage = localStorage;
		}

		private async Task AttachTokenAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}

		public async Task<List<NarrationDto>> GetByPoiIdAsync(int poiId)
		{
			await AttachTokenAsync(); // 👈 CHÍNH LÀ THIẾU DÒNG NÀY (Đính kèm token)

			try
			{
				return await _http.GetFromJsonAsync<List<NarrationDto>>($"api/Narrations/poi/{poiId}") ?? new();
			}
			catch
			{
				return new();
			}
		}

		public async Task<(bool Success, string Message)> CreateAsync(CreateNarrationDto req)
		{
			await AttachTokenAsync();
			var res = await _http.PostAsJsonAsync("api/Narrations", req);
			if (res.IsSuccessStatusCode) return (true, "Thành công");

			// Lấy nội dung lỗi từ Server trả về (ví dụ: "Ngôn ngữ này đã có thuyết minh!")
			var error = await res.Content.ReadAsStringAsync();
			return (false, error);
		}

		public async Task<(bool Success, string Message)> UpdateAsync(int id, UpdateNarrationDto req)
		{
			await AttachTokenAsync();
			var res = await _http.PutAsJsonAsync($"api/Narrations/{id}", req);
			if (res.IsSuccessStatusCode) return (true, "Thành công");

			var error = await res.Content.ReadAsStringAsync();
			return (false, error);
		}

		public async Task<bool> DeleteAsync(int id)
		{
			await AttachTokenAsync();
			var res = await _http.DeleteAsync($"api/Narrations/{id}");
			return res.IsSuccessStatusCode;
		}

		// 👇 HÀM BẮN FILE .MP3 LÊN CONTROLLER CỦA BẠN
		public async Task<bool> UploadAudioAsync(int narrationId, IBrowserFile file)
		{
			await AttachTokenAsync();
			using var content = new MultipartFormDataContent();

			// Cho phép upload file tối đa 50MB
			var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024));
			fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

			content.Add(fileContent, "file", file.Name);

			var res = await _http.PostAsync($"api/Narrations/{narrationId}/audio", content);
			return res.IsSuccessStatusCode;
		}
	}
}