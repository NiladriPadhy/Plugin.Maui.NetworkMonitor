using Microsoft.Extensions.Logging;

namespace Maui.NetworkMonitor.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddNetworkMonitor(options =>
        {
            options.StartAutomatically = true;
            options.EnableHttpProbe = true;
            options.EnableCaptivePortalDetection = true;
            options.ReprobeInterval = TimeSpan.FromSeconds(20);
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
