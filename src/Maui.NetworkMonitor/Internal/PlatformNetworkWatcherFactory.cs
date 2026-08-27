namespace Maui.NetworkMonitor.Internal;

internal static class PlatformNetworkWatcherFactory
{
    public static IPlatformNetworkWatcher Create()
    {
#if ANDROID
        return new AndroidNetworkWatcher();
#elif IOS
        return new IosNetworkWatcher();
#else
        return new FallbackNetworkWatcher();
#endif
    }
}
