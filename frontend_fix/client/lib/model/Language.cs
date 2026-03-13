using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class Language
    {
        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = string.Empty;

        [JsonPropertyName("languageName")]
        public string LanguageName { get; set; } = string.Empty;
    }
}
