using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Server.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            // Lấy thông tin cấu hình từ appsettings.json
            var account = new Account(
                config["CloudinarySettings:CloudName"],
                config["CloudinarySettings:ApiKey"],
                config["CloudinarySettings:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("File ảnh không hợp lệ hoặc trống.");

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "tourism_images", // Thư mục lưu ảnh trên Cloudinary
                UseFilename = true,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Lỗi upload ảnh lên Cloudinary: {uploadResult.Error.Message}");

            return new CloudinaryUploadResult
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId
            };
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return false;

            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }

        public async Task<CloudinaryUploadResult> UploadAudioAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("File âm thanh không hợp lệ hoặc trống.");

            using var stream = file.OpenReadStream();

            // LƯU Ý: API của Cloudinary dùng chung VideoUploadParams cho cả Video và Audio
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "tourism_audios", // Thư mục lưu file audio trên Cloudinary
                UseFilename = true,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Lỗi upload audio lên Cloudinary: {uploadResult.Error.Message}");

            return new CloudinaryUploadResult
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId
            };
        }

        public async Task<bool> DeleteAudioAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return false;

            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Video // BẮT BUỘC: Khai báo là Video để xóa được file Audio
            };
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }
    }
}