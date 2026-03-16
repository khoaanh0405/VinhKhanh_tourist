using client.lib.screens;
using client.lib.screens.home;
using client.lib.screens.map;
using client.lib.screens.qr;
using client.lib.services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Plugin.Maui.Audio;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader();
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<AudioService>();
            builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);
            builder.Services.AddSingleton<GeofenceService>();

            builder.Services.AddSingleton<AppViewModel>();
            builder.Services.AddTransient<HomeViewModel>();

            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<QrScannerPage>();
            builder.Services.AddTransient<FavoritesPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}