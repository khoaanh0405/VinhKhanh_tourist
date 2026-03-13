using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;

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
        Task<CloudinaryUploadResult> UploadAudioAsync(IFormFile file);
        Task<bool> DeleteAudioAsync(string publicId);
    }
}
