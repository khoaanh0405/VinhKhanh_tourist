using Microsoft.Maui.Graphics;

namespace client.lib.core
{
	public static class AppConstants
	{
		public const string AppName = "Vĩnh Khánh Food Street";

		public static readonly string BaseUrl = "https://sln71gls-7284.asse.devtunnels.ms";

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
		public const string Tts = "Tts";
	}
}