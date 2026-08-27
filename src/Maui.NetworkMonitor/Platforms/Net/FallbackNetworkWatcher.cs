using System.Net.NetworkInformation;
using Maui.NetworkMonitor.Internal;

namespace Maui.NetworkMonitor;

internal sealed class FallbackNetworkWatcher : IPlatformNetworkWatcher
{
    private readonly object _gate = new();
    private bool _started;
    private PlatformSnapshot _current = PlatformSnapshot.Empty;

    public event EventHandler<PlatformSnapshot>? Changed;

    public PlatformSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            NetworkChange.NetworkAddressChanged += OnNetworkChange;
            NetworkChange.NetworkAvailabilityChanged += OnAvailabilityChanged;
            _started = true;
        }

        Publish(ReadSnapshot());
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!_started)
            {
                return;
            }

            NetworkChange.NetworkAddressChanged -= OnNetworkChange;
            NetworkChange.NetworkAvailabilityChanged -= OnAvailabilityChanged;
            _started = false;
        }
    }

    public void Dispose() => Stop();

    private void OnAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) =>
        Publish(ReadSnapshot());

    private void OnNetworkChange(object? sender, EventArgs e) => Publish(ReadSnapshot());

    private void Publish(PlatformSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_current == snapshot)
            {
                return;
            }

            _current = snapshot;
        }

        Changed?.Invoke(this, snapshot);
    }

    private static PlatformSnapshot ReadSnapshot()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(IsUsable)
                .ToArray();

            if (interfaces.Length == 0)
            {
                return PlatformSnapshot.Empty;
            }

            var transports = interfaces
                .Select(MapTransport)
                .Distinct()
                .ToArray();

            return new PlatformSnapshot(
                IsConnected: true,
                HasNativeInternetCapability: true,
                IsNativeValidated: false,
                IsNativeCaptivePortal: false,
                Transports: transports,
                IsExpensive: transports.Contains(NetworkTransport.Cellular),
                IsConstrained: false,
                InterfaceName: interfaces[0].Name);
        }
        catch (NetworkInformationException)
        {
            return PlatformSnapshot.Empty;
        }
    }

    private static bool IsUsable(NetworkInterface networkInterface) =>
        networkInterface.OperationalStatus == OperationalStatus.Up
        && networkInterface.NetworkInterfaceType is not NetworkInterfaceType.Loopback
        && networkInterface.NetworkInterfaceType is not NetworkInterfaceType.Tunnel;

    private static NetworkTransport MapTransport(NetworkInterface networkInterface) =>
        networkInterface.NetworkInterfaceType switch
        {
            NetworkInterfaceType.Wireless80211 => NetworkTransport.WiFi,
            NetworkInterfaceType.Ppp
                or NetworkInterfaceType.Wman
                or NetworkInterfaceType.Wwanpp
                or NetworkInterfaceType.Wwanpp2 => NetworkTransport.Cellular,
            NetworkInterfaceType.Ethernet
                or NetworkInterfaceType.Ethernet3Megabit
                or NetworkInterfaceType.FastEthernetFx
                or NetworkInterfaceType.FastEthernetT
                or NetworkInterfaceType.GigabitEthernet => NetworkTransport.Ethernet,
            _ => NetworkTransport.Other
        };
}
