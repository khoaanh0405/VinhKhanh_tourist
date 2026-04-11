namespace VinhKhanh.WebAdmin.Models
{
	// 1. DTO cho Món ăn
	public class FoodTranslationDto
	{
		public int TranslationId { get; set; }
		public int FoodId { get; set; }
		public string LanguageCode { get; set; } = "en";
		public string Name { get; set; } = "";
		public string Description { get; set; } = "";
	}

	// 2. DTO cho Giới thiệu quán (ĐÃ ĐƯA RA NGOÀI)
	public class PoiTranslationDto
	{
		public int TranslationId { get; set; }
		public int PoiId { get; set; }
		public string LanguageCode { get; set; } = "en";
		public string Description { get; set; } = "";
	}

	// 3. Request cho Món ăn
	public class CreateFoodTranslationReq
	{
		public int FoodId { get; set; }
		public string LanguageCode { get; set; } = "en";
		public string Name { get; set; } = "";
		public string Description { get; set; } = "";

		public CreateFoodTranslationReq() { }

		public CreateFoodTranslationReq(int foodId, string languageCode, string name, string description)
		{
			FoodId = foodId;
			LanguageCode = languageCode;
			Name = name;
			Description = description;
		}
	}
}