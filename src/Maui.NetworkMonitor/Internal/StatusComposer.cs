namespace Maui.NetworkMonitor.Internal;

internal static class StatusComposer
{
    public static NetworkStatus Compose(PlatformSnapshot snapshot, ProbeResult? probe)
    {
        if (!snapshot.IsConnected)
        {
            return new NetworkStatus
            {
                Timestamp = DateTimeOffset.UtcNow,
                IsConnected = false,
                HasInternet = false,
                IsCaptivePortal = false,
                Reachability = InternetReachability.Offline,
                PrimaryTransport = NetworkTransport.None,
                ActiveTransports = [NetworkTransport.None],
                IsExpensive = false,
                IsConstrained = false,
                InterfaceName = snapshot.InterfaceName
            };
        }

        var transports = NormalizeTransports(snapshot.Transports);
        var primary = PickPrimary(transports);

        var captive = snapshot.IsNativeCaptivePortal;
        var internet = false;

        if (probe?.Outcome == ProbeOutcome.Internet)
        {
            internet = true;
            captive = false;
        }
        else if (probe?.Outcome == ProbeOutcome.CaptivePortal)
        {
            captive = true;
            internet = false;
        }
        else if (snapshot.IsNativeCaptivePortal)
        {
            captive = true;
            internet = false;
        }
        else if (snapshot.IsNativeValidated)
        {
            internet = true;
        }

        InternetReachability reachability;
        if (internet)
        {
            reachability = InternetReachability.Internet;
        }
        else if (captive)
        {
            reachability = InternetReachability.CaptivePortal;
        }
        else
        {
            reachability = InternetReachability.LocalNetworkOnly;
        }

        return new NetworkStatus
        {
            Timestamp = DateTimeOffset.UtcNow,
            IsConnected = true,
            HasInternet = internet,
            IsCaptivePortal = captive,
            Reachability = reachability,
            PrimaryTransport = primary,
            ActiveTransports = transports,
            IsExpensive = snapshot.IsExpensive || primary == NetworkTransport.Cellular,
            IsConstrained = snapshot.IsConstrained,
            InterfaceName = snapshot.InterfaceName
        };
    }

    internal static IReadOnlyList<NetworkTransport> NormalizeTransports(IReadOnlyList<NetworkTransport> transports)
    {
        if (transports.Count == 0)
        {
            return [NetworkTransport.Unknown];
        }

        return transports
            .Where(t => t != NetworkTransport.None)
            .Distinct()
            .OrderBy(Priority)
            .ToArray();
    }

    internal static NetworkTransport PickPrimary(IReadOnlyList<NetworkTransport> transports)
    {
        if (transports.Count == 0)
        {
            return NetworkTransport.Unknown;
        }

        return transports.OrderBy(Priority).First();
    }

    private static int Priority(NetworkTransport transport) => transport switch
    {
        NetworkTransport.WiFi => 0,
        NetworkTransport.Ethernet => 1,
        NetworkTransport.Cellular => 2,
        NetworkTransport.Other => 3,
        NetworkTransport.Unknown => 4,
        _ => 5
    };
}
