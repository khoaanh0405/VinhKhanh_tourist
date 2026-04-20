using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    // ─── UI TRANSLATIONS (Bảng mới) ─────────────────────────────────────────────

    /// <summary>
    /// Bản dịch giao diện động. Map với bảng UITranslations.
    /// Mỗi dòng là một cặp (LanguageCode, ResourceKey) → ResourceValue.
    /// </summary>
    public class UITranslationDto
    {
        public int TranslationId { get; set; }

        [Required]
        public string LanguageCode { get; set; } = string.Empty;

        /// <summary>Key định danh chuỗi UI, ví dụ: "Btn_Login", "Title_Home".</summary>
        [Required]
        public string ResourceKey { get; set; } = string.Empty;

        /// <summary>Giá trị hiển thị tương ứng với ngôn ngữ đã chọn.</summary>
        [Required]
        public string ResourceValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trả về toàn bộ chuỗi UI cho một ngôn ngữ dưới dạng dictionary,
    /// tiện dùng phía client: { "Btn_Login": "Đăng nhập", ... }
    /// </summary>
    public class UITranslationBundleDto
    {
        public string LanguageCode { get; set; } = string.Empty;
        public Dictionary<string, string> Translations { get; set; } = new();
    }

    // Request records cho Admin quản lý UITranslations
    public record CreateUITranslationReq(
        [Required] string LanguageCode,
        [Required] string ResourceKey,
        [Required] string ResourceValue);

    public record UpdateUITranslationReq([Required] string ResourceValue);

    // ─── FOOD TRANSLATIONS ──────────────────────────────────────────────────────

    /// <summary>
    /// Bản dịch tên món ăn. Map với bảng FoodTranslations.
    /// Đã xoá: Description — bảng FoodTranslations chỉ có cột Name.
    /// Đã xoá: PoiTranslationDto, RestaurantTranslationDto — không có bảng tương ứng trong DB.
    /// </summary>
    public class FoodTranslationDto
    {
        public int TranslationId { get; set; }

        [Required]
        public int FoodId { get; set; }

        [Required]
        public string LanguageCode { get; set; } = string.Empty;

        /// <summary>Tên món ăn đã được dịch sang ngôn ngữ tương ứng.</summary>
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    // Request records cho Food Translations
    public record CreateFoodTranslationReq(
        [Required] int FoodId,
        [Required] string LanguageCode,
        [Required] string Name);

    public record UpdateFoodTranslationReq([Required] string Name);
}