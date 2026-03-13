using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    public class PoiImageUploadDto
    {
        [Required]
        public int PoiId { get; set; }

        [Required]
        public IFormFile File { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class PoiImageReorderDto
    {
        [Required]
        public int ImageId { get; set; }

        [Required]
        public int DisplayOrder { get; set; }
    }

    public class PoiImageResponseDto
    {
        public int ImageId { get; set; }
        public int PoiId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string PublicId { get; set; } // [Thêm mới] Trả về PublicId cho Client
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}