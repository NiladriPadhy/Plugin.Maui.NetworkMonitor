using Maui.NetworkMonitor.Internal;

namespace Maui.NetworkMonitor.Tests;

public sealed class StatusComposerTests
{
    [Fact]
    public void Disconnected_IsOffline()
    {
        var status = StatusComposer.Compose(PlatformSnapshot.Empty, null);

        Assert.False(status.IsConnected);
        Assert.False(status.HasInternet);
        Assert.Equal(InternetReachability.Offline, status.Reachability);
        Assert.Equal(NetworkTransport.None, status.PrimaryTransport);
    }

    [Fact]
    public void ConnectedWithoutValidation_IsLocalNetworkOnly()
    {
        var snapshot = Connected(NetworkTransport.WiFi);
        var status = StatusComposer.Compose(snapshot, ProbeResult.Unreachable());

        Assert.True(status.IsConnected);
        Assert.False(status.HasInternet);
        Assert.Equal(InternetReachability.LocalNetworkOnly, status.Reachability);
        Assert.True(status.IsWiFi);
    }

    [Fact]
    public void NativeCaptivePortal_IsDetectedWithoutProbe()
    {
        var snapshot = Connected(NetworkTransport.WiFi) with { IsNativeCaptivePortal = true };
        var status = StatusComposer.Compose(snapshot, null);

        Assert.True(status.IsCaptivePortal);
        Assert.False(status.HasInternet);
        Assert.Equal(InternetReachability.CaptivePortal, status.Reachability);
    }

    [Fact]
    public void ProbeInternet_ClearsNativeCaptivePortal()
    {
        var snapshot = Connected(NetworkTransport.WiFi) with { IsNativeCaptivePortal = true };
        var status = StatusComposer.Compose(snapshot, ProbeResult.Internet("gstatic 204"));

        Assert.True(status.HasInternet);
        Assert.False(status.IsCaptivePortal);
        Assert.Equal(InternetReachability.Internet, status.Reachability);
    }

    [Fact]
    public void ProbeCaptive_WinsOverNativeValidated()
    {
        var snapshot = Connected(NetworkTransport.Cellular) with { IsNativeValidated = true };
        var status = StatusComposer.Compose(snapshot, ProbeResult.Captive("redirect"));

        Assert.True(status.IsCaptivePortal);
        Assert.False(status.HasInternet);
        Assert.True(status.IsCellular);
        Assert.True(status.IsExpensive);
    }

    [Fact]
    public void NativeValidatedWithoutProbe_IsInternet()
    {
        var snapshot = Connected(NetworkTransport.WiFi) with { IsNativeValidated = true };
        var status = StatusComposer.Compose(snapshot, null);

        Assert.True(status.HasInternet);
        Assert.Equal(InternetReachability.Internet, status.Reachability);
    }

    [Fact]
    public void WifiAndCellular_PrefersWifi()
    {
        var snapshot = new PlatformSnapshot(
            IsConnected: true,
            HasNativeInternetCapability: true,
            IsNativeValidated: true,
            IsNativeCaptivePortal: false,
            Transports: [NetworkTransport.Cellular, NetworkTransport.WiFi],
            IsExpensive: false,
            IsConstrained: false,
            InterfaceName: "wlan0");

        var status = StatusComposer.Compose(snapshot, ProbeResult.Internet());

        Assert.Equal(NetworkTransport.WiFi, status.PrimaryTransport);
        Assert.Contains(NetworkTransport.Cellular, status.ActiveTransports);
        Assert.Contains(NetworkTransport.WiFi, status.ActiveTransports);
    }

    private static PlatformSnapshot Connected(NetworkTransport transport) =>
        new(
            IsConnected: true,
            HasNativeInternetCapability: true,
            IsNativeValidated: false,
            IsNativeCaptivePortal: false,
            Transports: [transport],
            IsExpensive: transport == NetworkTransport.Cellular,
            IsConstrained: false,
            InterfaceName: "test0");
}
