using Maui.NetworkMonitor.Internal;

namespace Maui.NetworkMonitor.Tests;

public sealed class ChangeKindDetectorTests
{
    [Fact]
    public void OfflineToInternet_IsBecameOnline()
    {
        var kind = ChangeKindDetector.Detect(NetworkStatus.Offline, Internet(NetworkTransport.WiFi));
        Assert.Equal(NetworkChangeKind.BecameOnline, kind);
    }

    [Fact]
    public void InternetToOffline_IsBecameOffline()
    {
        var kind = ChangeKindDetector.Detect(Internet(NetworkTransport.WiFi), NetworkStatus.Offline);
        Assert.Equal(NetworkChangeKind.BecameOffline, kind);
    }

    [Fact]
    public void WifiToCellular_IsTransportChanged()
    {
        var kind = ChangeKindDetector.Detect(
            Internet(NetworkTransport.WiFi),
            Internet(NetworkTransport.Cellular));

        Assert.Equal(NetworkChangeKind.TransportChanged, kind);
    }

    [Fact]
    public void CellularToWifi_IsTransportChanged()
    {
        var kind = ChangeKindDetector.Detect(
            Internet(NetworkTransport.Cellular),
            Internet(NetworkTransport.WiFi));

        Assert.Equal(NetworkChangeKind.TransportChanged, kind);
    }

    [Fact]
    public void InternetToCaptive_IsCaptivePortalDetected()
    {
        var kind = ChangeKindDetector.Detect(Internet(NetworkTransport.WiFi), Captive(NetworkTransport.WiFi));
        Assert.Equal(NetworkChangeKind.CaptivePortalDetected, kind);
    }

    [Fact]
    public void CaptiveToInternet_IsCaptivePortalCleared()
    {
        var kind = ChangeKindDetector.Detect(Captive(NetworkTransport.WiFi), Internet(NetworkTransport.WiFi));
        Assert.Equal(NetworkChangeKind.CaptivePortalCleared, kind);
    }

    private static NetworkStatus Internet(NetworkTransport transport) =>
        new()
        {
            IsConnected = true,
            HasInternet = true,
            Reachability = InternetReachability.Internet,
            PrimaryTransport = transport,
            ActiveTransports = [transport]
        };

    private static NetworkStatus Captive(NetworkTransport transport) =>
        new()
        {
            IsConnected = true,
            IsCaptivePortal = true,
            Reachability = InternetReachability.CaptivePortal,
            PrimaryTransport = transport,
            ActiveTransports = [transport]
        };
}
