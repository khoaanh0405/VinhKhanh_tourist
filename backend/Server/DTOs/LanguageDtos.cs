namespace Server.DTOs
{
    public class LanguageDto
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;

        /// <summary>
        /// Số Narration hiện có cho ngôn ngữ này (computed, không phải cột DB).
        /// </summary>
        public int NarrationCount { get; set; }
    }
}