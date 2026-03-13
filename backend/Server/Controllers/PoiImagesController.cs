using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;
using Server.Services; 

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<PoiImagesController> _logger;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public PoiImagesController(
            AppDbContext context,
            ICloudinaryService cloudinaryService,
            ILogger<PoiImagesController> logger)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        [HttpGet("{poiId}")]
        public async Task<ActionResult<List<PoiImageResponseDto>>> GetByPoi(int poiId)
        {
            var images = await _context.PoiImages
                .AsNoTracking()
                .Where(i => i.PoiId == poiId)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new PoiImageResponseDto
                {
                    ImageId = i.ImageId,
                    PoiId = i.PoiId,
                    ImageUrl = i.ImageUrl,
                    // Nếu DTO có PublicId thì thêm vào đây, nếu không thì bỏ qua
                    DisplayOrder = i.DisplayOrder,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            return Ok(images);
        }

        [HttpPost("upload")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<ActionResult<PoiImageResponseDto>> UploadImage([FromForm] PoiImageUploadDto dto)
        {
            if (dto.File == null || dto.File.Length == 0) return BadRequest("Vui lòng chọn file.");
            if (dto.File.Length > MaxFileSizeBytes) return BadRequest($"File vượt quá {MaxFileSizeBytes / 1024 / 1024} MB.");

            var extension = Path.GetExtension(dto.File.FileName);
            if (!AllowedExtensions.Contains(extension))
                return BadRequest($"Định dạng không hỗ trợ: {string.Join(", ", AllowedExtensions)}");

            if (!await _context.POIs.AnyAsync(p => p.PoiId == dto.PoiId))
                return NotFound($"Không tìm thấy POI với Id = {dto.PoiId}");

            try
            {
                // Upload lên Cloudinary (Lấy về cả URL và PublicId)
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.File);

                var newImage = new PoiImage
                {
                    PoiId = dto.PoiId,
                    ImageUrl = uploadResult.Url,
                    PublicId = uploadResult.PublicId, // LƯU PUBLIC ID
                    DisplayOrder = dto.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PoiImages.Add(newImage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Uploaded image {ImageId} to Cloudinary for POI {PoiId}", newImage.ImageId, dto.PoiId);

                return Ok(new PoiImageResponseDto
                {
                    ImageId = newImage.ImageId,
                    PoiId = newImage.PoiId,
                    ImageUrl = newImage.ImageUrl,
                    DisplayOrder = newImage.DisplayOrder,
                    CreatedAt = newImage.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh cho POI {PoiId}", dto.PoiId);
                return StatusCode(500, "Lỗi server khi upload ảnh.");
            }
        }

        [HttpPost("upload-multiple")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<ActionResult<List<PoiImageResponseDto>>> UploadMultiple(
            [FromForm] int poiId,
            [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0) return BadRequest("Vui lòng chọn ít nhất một file.");
            if (!await _context.POIs.AnyAsync(p => p.PoiId == poiId)) return NotFound($"Không tìm thấy POI với Id = {poiId}");

            var maxOrder = await _context.PoiImages.Where(i => i.PoiId == poiId).MaxAsync(i => (int?)i.DisplayOrder) ?? -1;
            var results = new List<PoiImageResponseDto>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file.Length > MaxFileSizeBytes || !AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
                {
                    errors.Add($"{file.FileName}: Kích thước hoặc định dạng không hợp lệ.");
                    continue;
                }

                try
                {
                    maxOrder++;
                    // Upload lên Cloudinary
                    var uploadResult = await _cloudinaryService.UploadImageAsync(file);

                    var newImage = new PoiImage
                    {
                        PoiId = poiId,
                        ImageUrl = uploadResult.Url,
                        PublicId = uploadResult.PublicId, // LƯU PUBLIC ID
                        DisplayOrder = maxOrder,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PoiImages.Add(newImage);
                    await _context.SaveChangesAsync();

                    results.Add(new PoiImageResponseDto
                    {
                        ImageId = newImage.ImageId,
                        PoiId = newImage.PoiId,
                        ImageUrl = newImage.ImageUrl,
                        DisplayOrder = newImage.DisplayOrder,
                        CreatedAt = newImage.CreatedAt
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi upload file {FileName}", file.FileName);
                    errors.Add($"{file.FileName}: lỗi khi upload.");
                }
            }

            return Ok(new { Uploaded = results, Errors = errors });
        }

        [HttpPatch("reorder")]
        public async Task<IActionResult> Reorder([FromBody] List<PoiImageReorderDto> items)
        {
            // (Giữ nguyên, không liên quan đến Cloudinary)
            if (items == null || items.Count == 0) return BadRequest("Danh sách reorder trống.");
            var ids = items.Select(i => i.ImageId).ToList();
            var images = await _context.PoiImages.Where(i => ids.Contains(i.ImageId)).ToListAsync();

            foreach (var item in items)
            {
                var image = images.FirstOrDefault(i => i.ImageId == item.ImageId);
                if (image != null)
                {
                    image.DisplayOrder = item.DisplayOrder;
                    image.UpdatedAt = DateTime.UtcNow;
                }
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var image = await _context.PoiImages.FindAsync(id);
            if (image == null) return NotFound($"Không tìm thấy ảnh với Id = {id}");

            try
            {
                // 1. Xóa ảnh trên Cloudinary dựa vào PublicId
                if (!string.IsNullOrEmpty(image.PublicId))
                {
                    await _cloudinaryService.DeleteImageAsync(image.PublicId);
                }

                // 2. Xóa record trong DB
                _context.PoiImages.Remove(image);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted image {ImageId} (POI {PoiId})", id, image.PoiId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa ảnh {ImageId}", id);
                return StatusCode(500, "Lỗi server khi xóa ảnh.");
            }
        }
    }
}