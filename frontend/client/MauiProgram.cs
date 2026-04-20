using client.lib.screens;
using client.lib.screens.home;
using client.lib.screens.map;
using client.lib.screens.qr;
using client.lib.services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            SQLitePCL.Batteries_V2.Init();

            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    // Chuyển phần khai báo font vào bên trong đây
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<AudioService>();
            builder.Services.AddSingleton<GeofenceService>();

            builder.Services.AddTransient<HomeViewModel>();

            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<QrScannerPage>();

            builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri("  https://sln71gls-7284.asse.devtunnels.ms") });
            builder.Services.AddSingleton<TrackingService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<LocalDbService>();

            return builder.Build();
        }
    }
}