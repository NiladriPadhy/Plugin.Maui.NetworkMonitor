using System.Net.Http;
using Maui.NetworkMonitor.Internal;

namespace Maui.NetworkMonitor.Tests;

public sealed class NetworkMonitorTests
{
    [Fact]
    public async Task Refresh_PublishesTransportChange()
    {
        var watcher = new FakeWatcher(Connected(NetworkTransport.WiFi, validated: true));
        var options = new NetworkMonitorOptions { EnableHttpProbe = false, StartAutomatically = false };
        using var monitor = new NetworkMonitor(options, watcher, new InternetProbe(options, new NoHttpHandler()), ownsDependencies: true);

        var events = new List<NetworkStatusChangedEventArgs>();
        monitor.StatusChanged += (_, args) => events.Add(args);

        var first = await monitor.RefreshAsync();
        Assert.True(first.IsWiFi);
        Assert.True(first.HasInternet);

        watcher.Emit(Connected(NetworkTransport.Cellular, validated: true));
        var second = await monitor.RefreshAsync();

        Assert.True(second.IsCellular);
        Assert.Contains(events, e => e.ChangeKind == NetworkChangeKind.TransportChanged && e.IsTransportTransition);
    }

    [Fact]
    public void Options_RejectEmptyProbes()
    {
        var options = new NetworkMonitorOptions();
        options.ProbeEndpoints.Clear();

        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void ProbeEndpoint_RejectsRelativeUrl()
    {
        Assert.Throws<ArgumentException>(() => ProbeEndpoint.Generate204("/generate_204"));
    }

    private static PlatformSnapshot Connected(NetworkTransport transport, bool validated) =>
        new(
            IsConnected: true,
            HasNativeInternetCapability: true,
            IsNativeValidated: validated,
            IsNativeCaptivePortal: false,
            Transports: [transport],
            IsExpensive: transport == NetworkTransport.Cellular,
            IsConstrained: false,
            InterfaceName: "fake0");

    private sealed class FakeWatcher : IPlatformNetworkWatcher
    {
        public FakeWatcher(PlatformSnapshot current) => Current = current;

        public event EventHandler<PlatformSnapshot>? Changed;

        public PlatformSnapshot Current { get; private set; }

        public void Emit(PlatformSnapshot snapshot)
        {
            Current = snapshot;
            Changed?.Invoke(this, snapshot);
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class NoHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("HTTP should be disabled in this test.");
    }
}
