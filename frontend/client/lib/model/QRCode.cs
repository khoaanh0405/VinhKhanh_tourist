using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class QRCode
    {
        [JsonPropertyName("qrCodeId")]
        public int QrCodeId { get; set; }

        [JsonPropertyName("codeValue")]
        public string CodeValue { get; set; } = string.Empty;

        [JsonPropertyName("poiId")]
        public int? PoiId { get; set; }
    }
}
