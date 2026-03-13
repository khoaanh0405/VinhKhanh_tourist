using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class AudioFile
    {
        [JsonPropertyName("audioId")]
        public int AudioId { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; } = "mp3";
    }
}
