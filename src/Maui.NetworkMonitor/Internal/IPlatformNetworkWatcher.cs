namespace Maui.NetworkMonitor.Internal;

internal interface IPlatformNetworkWatcher : IDisposable
{
    event EventHandler<PlatformSnapshot>? Changed;

    PlatformSnapshot Current { get; }

    void Start();

    void Stop();
}
