namespace Maui.NetworkMonitor.Internal;

internal static class ChangeKindDetector
{
    public static NetworkChangeKind Detect(NetworkStatus previous, NetworkStatus current)
    {
        if (previous.Reachability is InternetReachability.Unknown or InternetReachability.Offline
            && current.HasInternet)
        {
            return NetworkChangeKind.BecameOnline;
        }

        if (previous.IsConnected && !current.IsConnected)
        {
            return NetworkChangeKind.BecameOffline;
        }

        if (!previous.IsCaptivePortal && current.IsCaptivePortal)
        {
            return NetworkChangeKind.CaptivePortalDetected;
        }

        if (previous.IsCaptivePortal && !current.IsCaptivePortal)
        {
            return current.HasInternet
                ? NetworkChangeKind.CaptivePortalCleared
                : NetworkChangeKind.BecameOffline;
        }

        if (previous.PrimaryTransport != current.PrimaryTransport
            && previous.PrimaryTransport is not (NetworkTransport.None or NetworkTransport.Unknown)
            && current.PrimaryTransport is not (NetworkTransport.None or NetworkTransport.Unknown))
        {
            return NetworkChangeKind.TransportChanged;
        }

        if (previous.Reachability != current.Reachability
            || previous.HasInternet != current.HasInternet
            || previous.IsExpensive != current.IsExpensive
            || previous.IsConstrained != current.IsConstrained)
        {
            return NetworkChangeKind.QualityChanged;
        }

        if (!previous.IsConnected && current.IsConnected)
        {
            return current.HasInternet
                ? NetworkChangeKind.BecameOnline
                : NetworkChangeKind.QualityChanged;
        }

        return NetworkChangeKind.Unknown;
    }
}
