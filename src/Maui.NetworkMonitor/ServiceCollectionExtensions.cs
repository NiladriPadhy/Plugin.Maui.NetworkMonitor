using Microsoft.Extensions.DependencyInjection;

namespace Maui.NetworkMonitor;

/// <summary>
/// Dependency-injection helpers for <see cref="INetworkMonitor"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="INetworkMonitor"/>.
    /// In a MAUI app call this from <c>MauiProgram</c> as <c>builder.Services.AddNetworkMonitor()</c>.
    /// </summary>
    public static IServiceCollection AddNetworkMonitor(
        this IServiceCollection services,
        Action<NetworkMonitorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<INetworkMonitor>(_ =>
        {
            var options = new NetworkMonitorOptions();
            configure?.Invoke(options);
            var monitor = new NetworkMonitor(options);
            if (options.StartAutomatically)
            {
                monitor.Start();
            }

            return monitor;
        });

        return services;
    }
}
