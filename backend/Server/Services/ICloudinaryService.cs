using Microsoft.AspNetCore.Http;

namespace Server.Services
{
    public class CloudinaryUploadResult
    {
        public string Url { get; set; }
        public string PublicId { get; set; }
    }

    public interface ICloudinaryService
    {
        Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string publicId);
    }
}