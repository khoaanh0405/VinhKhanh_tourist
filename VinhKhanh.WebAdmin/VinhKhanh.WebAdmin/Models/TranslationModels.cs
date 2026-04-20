namespace VinhKhanh.WebAdmin.Models
{
    public class FoodTranslationDto
    {
        public int TranslationId { get; set; }
        public int FoodId { get; set; }
        public string LanguageCode { get; set; } = "en";
        public string Name { get; set; } = "";
    }

    public class CreateFoodTranslationReq
    {
        public int FoodId { get; set; }
        public string LanguageCode { get; set; } = "en";
        public string Name { get; set; } = "";

        public CreateFoodTranslationReq() { }

        public CreateFoodTranslationReq(int foodId, string languageCode, string name)
        {
            FoodId = foodId;
            LanguageCode = languageCode;
            Name = name;
        }
    }
}