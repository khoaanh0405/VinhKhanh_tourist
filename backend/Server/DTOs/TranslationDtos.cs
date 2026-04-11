using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    // DTO cho POI Translations
    public class PoiTranslationDto
    {
        public int TranslationId { get; set; }
        [Required]
        public int PoiId { get; set; }
        [Required]
        public string LanguageCode { get; set; }
        [Required]
        public string Description { get; set; }
    }

    // DTO cho Restaurant Translations
    public class RestaurantTranslationDto
    {
        public int TranslationId { get; set; }
        [Required]
        public int RestaurantId { get; set; }
        [Required]
        public string LanguageCode { get; set; }
        [Required]
        public string Description { get; set; }
    }

    // DTO cho Food Translations
    public class FoodTranslationDto
    {
        public int TranslationId { get; set; }
        [Required]
        public int FoodId { get; set; }
        [Required]
        public string LanguageCode { get; set; }
        [Required]
        public string Name { get; set; } // Món ăn cần dịch cả tên
        [Required]
        public string Description { get; set; }
    }
	// Bổ sung dòng này xuống dưới cùng (trước dấu ngoặc nhọn } đóng namespace)
	public record CreateFoodTranslationReq(int FoodId, string LanguageCode, string Name, string Description);
	public record CreatePoiTranslationReq(int PoiId, string LanguageCode, string Description);
}