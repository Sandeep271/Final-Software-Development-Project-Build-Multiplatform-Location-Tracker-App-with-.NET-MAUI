using LocationHeatMapApp.Data;
using LocationHeatMapApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace LocationHeatMapApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiMaps();

        // Register application services with the built-in dependency-injection container.
        builder.Services.AddSingleton<LocationDatabase>();
        builder.Services.AddSingleton<HeatMapService>();
        builder.Services.AddSingleton<LocationTrackingService>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
