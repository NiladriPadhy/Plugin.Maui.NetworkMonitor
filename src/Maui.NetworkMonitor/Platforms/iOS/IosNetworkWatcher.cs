using CoreFoundation;
using Maui.NetworkMonitor.Internal;
using Network;

namespace Maui.NetworkMonitor;

internal sealed class IosNetworkWatcher : IPlatformNetworkWatcher
{
    private readonly object _gate = new();
    private readonly DispatchQueue _queue = new("com.mauiessentials.maui.networkmonitor");
    private NWPathMonitor? _monitor;
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
            if (_monitor is not null)
            {
                return;
            }

            var monitor = new NWPathMonitor();
            monitor.SetQueue(_queue);
            monitor.SnapshotHandler = OnPathChanged;
            _monitor = monitor;
            monitor.Start();
        }
    }

    public void Stop()
    {
        NWPathMonitor? monitor;
        lock (_gate)
        {
            monitor = _monitor;
            _monitor = null;
        }

        if (monitor is null)
        {
            return;
        }

        monitor.Cancel();
        monitor.Dispose();
    }

    public void Dispose() => Stop();

    private void OnPathChanged(NWPath path)
    {
        var snapshot = ToSnapshot(path);
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

    private static PlatformSnapshot ToSnapshot(NWPath path)
    {
        var connected = path.Status == NWPathStatus.Satisfied;
        if (!connected)
        {
            return PlatformSnapshot.Empty with { InterfaceName = FirstInterfaceName(path) };
        }

        var transports = new List<NetworkTransport>(3);
        if (path.UsesInterfaceType(NWInterfaceType.Wifi))
        {
            transports.Add(NetworkTransport.WiFi);
        }

        if (path.UsesInterfaceType(NWInterfaceType.Cellular))
        {
            transports.Add(NetworkTransport.Cellular);
        }

        if (path.UsesInterfaceType(NWInterfaceType.Wired))
        {
            transports.Add(NetworkTransport.Ethernet);
        }

        if (path.UsesInterfaceType(NWInterfaceType.Other))
        {
            transports.Add(NetworkTransport.Other);
        }

        if (transports.Count == 0)
        {
            transports.Add(NetworkTransport.Unknown);
        }

        // iOS reports Satisfied for many captive portals. Native validation is optimistic;
        // HTTP probes (and TLS interception) refine CaptivePortal vs Internet.
        return new PlatformSnapshot(
            IsConnected: true,
            HasNativeInternetCapability: true,
            IsNativeValidated: false,
            IsNativeCaptivePortal: false,
            Transports: transports,
            IsExpensive: path.IsExpensive,
            IsConstrained: path.IsConstrained,
            InterfaceName: FirstInterfaceName(path));
    }

    private static string? FirstInterfaceName(NWPath path)
    {
        string? name = null;
        try
        {
            path.EnumerateInterfaces(iface =>
            {
                name ??= iface.Name;
                return name is null;
            });
        }
        catch (Exception)
        {
            // Some OS versions expose used interfaces differently.
        }

        return name;
    }
}
