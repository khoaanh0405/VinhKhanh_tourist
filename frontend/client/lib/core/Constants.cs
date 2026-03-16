using Microsoft.Maui.Graphics;

namespace client.lib.core
{
    public static class AppConstants
    {
        public const string AppName = "Vĩnh Khánh Food Street";

        // 1. Tách riêng BaseUrl để dùng cho việc hiển thị ảnh từ wwwroot
        // Android dùng 10.0.2.2 để trỏ về localhost của máy tính
        public static readonly string BaseUrl =
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5280"
                : "http://localhost:5280";

        // 2. ApiBaseUrl sẽ kế thừa từ BaseUrl và thêm /api/
        public static readonly string ApiBaseUrl = $"{BaseUrl}/api/";


        public static readonly Color PrimaryColor = Color.FromArgb("#E63946");
        public static readonly Color SecondaryColor = Color.FromArgb("#F1FAEE");
        public static readonly Color AccentColor = Color.FromArgb("#A8DADC");
        public static readonly Color TextDark = Color.FromArgb("#1D3557");
        public static readonly Color BackgroundLight = Color.FromArgb("#FDFDFD");

        public const double DefaultGeofenceRadius = 50.0;
        public const double MapZoomLevel = 16.0;
    }

    public static class ApiEndpoints
    {
        public const string Pois = "POIs";
        public const string Restaurants = "Restaurant";
        public const string Foods = "Food";
        public const string Narrations = "Narrations";
        public const string Languages = "Language";
        public const string QrCodes = "QRCode";
        public const string Tts = "TTS";
        public const string Geofence = "Geofence";
        public const string Audio = "AudioFile";
    }
}