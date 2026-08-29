using Android.Content;
using Android.Net;
using Android.OS;
using Maui.NetworkMonitor.Internal;

namespace Maui.NetworkMonitor;

internal sealed class AndroidNetworkWatcher : IPlatformNetworkWatcher
{
    private readonly object _gate = new();
    private readonly Callback _callback;
    private ConnectivityManager? _connectivityManager;
    private bool _started;
    private int _registerGeneration;
    private PlatformSnapshot _current = PlatformSnapshot.Empty;

    public AndroidNetworkWatcher()
    {
        _callback = new Callback(this);
    }

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
        var context = Android.App.Application.Context
            ?? throw new InvalidOperationException("Android application context is not available.");

        var connectivity = context.GetSystemService(Context.ConnectivityService) as ConnectivityManager
            ?? throw new InvalidOperationException("ConnectivityManager is not available.");

        int generation;
        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            _connectivityManager = connectivity;
            _started = true;
            _registerGeneration++;
            generation = _registerGeneration;
        }

        try
        {
            RegisterCallback(connectivity);
        }
        catch
        {
            lock (_gate)
            {
                if (_registerGeneration == generation)
                {
                    _started = false;
                    _connectivityManager = null;
                }
            }

            throw;
        }

        lock (_gate)
        {
            if (!_started || _registerGeneration != generation)
            {
                try
                {
                    connectivity.UnregisterNetworkCallback(_callback);
                }
                catch (Java.Lang.IllegalArgumentException)
                {
                }

                return;
            }
        }

        Publish(ReadSnapshot(connectivity));
    }

    public void Stop()
    {
        ConnectivityManager? manager;
        lock (_gate)
        {
            if (!_started)
            {
                return;
            }

            manager = _connectivityManager;
            _started = false;
            _registerGeneration++;
            _connectivityManager = null;
        }

        if (manager is null)
        {
            return;
        }

        try
        {
            manager.UnregisterNetworkCallback(_callback);
        }
        catch (Java.Lang.IllegalArgumentException)
        {
            // Callback was not registered.
        }
    }

    public void Dispose() => Stop();

    private void RegisterCallback(ConnectivityManager connectivity)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                connectivity.RegisterDefaultNetworkCallback(_callback, new Handler(Looper.MainLooper!));
            }
            else
            {
                connectivity.RegisterDefaultNetworkCallback(_callback);
            }

            return;
        }

        var builder = new NetworkRequest.Builder();
        builder.AddCapability(NetCapability.Internet);
        var request = builder.Build() ?? throw new InvalidOperationException("Unable to create a network request.");
        connectivity.RegisterNetworkCallback(request, _callback);
    }

    private void OnNetworkEvent()
    {
        var manager = _connectivityManager;
        if (manager is null)
        {
            return;
        }

        Publish(ReadSnapshot(manager));
    }

    private void OnNetworkLost()
    {
        var manager = _connectivityManager;
        if (manager is null)
        {
            Publish(PlatformSnapshot.Empty);
            return;
        }

        Publish(ReadSnapshot(manager));
    }

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

    private static PlatformSnapshot ReadSnapshot(ConnectivityManager manager)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            var network = manager.ActiveNetwork;
            if (network is null)
            {
                return PlatformSnapshot.Empty;
            }

            var capabilities = manager.GetNetworkCapabilities(network);
            var link = manager.GetLinkProperties(network);
            if (capabilities is null)
            {
                return PlatformSnapshot.Empty;
            }

            return FromCapabilities(capabilities, link);
        }

        return ReadLegacySnapshot(manager);
    }

    private static PlatformSnapshot FromCapabilities(NetworkCapabilities capabilities, LinkProperties? link)
    {
        var transports = new List<NetworkTransport>(3);
        if (capabilities.HasTransport(TransportType.Wifi))
        {
            transports.Add(NetworkTransport.WiFi);
        }

        if (capabilities.HasTransport(TransportType.Cellular))
        {
            transports.Add(NetworkTransport.Cellular);
        }

        if (capabilities.HasTransport(TransportType.Ethernet))
        {
            transports.Add(NetworkTransport.Ethernet);
        }

        if (capabilities.HasTransport(TransportType.Bluetooth)
            || capabilities.HasTransport(TransportType.Vpn))
        {
            transports.Add(NetworkTransport.Other);
        }

        if (transports.Count == 0)
        {
            transports.Add(NetworkTransport.Unknown);
        }

        var hasInternet = capabilities.HasCapability(NetCapability.Internet);
        var validated = OperatingSystem.IsAndroidVersionAtLeast(23)
            && capabilities.HasCapability(NetCapability.Validated);
        var captive = OperatingSystem.IsAndroidVersionAtLeast(23)
            && capabilities.HasCapability(NetCapability.CaptivePortal);
        var metered = !capabilities.HasCapability(NetCapability.NotMetered);
        var constrained = OperatingSystem.IsAndroidVersionAtLeast(30)
            && capabilities.HasCapability(NetCapability.NotCongested) == false;

        return new PlatformSnapshot(
            IsConnected: true,
            HasNativeInternetCapability: hasInternet,
            IsNativeValidated: validated,
            IsNativeCaptivePortal: captive,
            Transports: transports,
            IsExpensive: metered || capabilities.HasTransport(TransportType.Cellular),
            IsConstrained: constrained,
            InterfaceName: link?.InterfaceName);
    }

#pragma warning disable CS0618, CA1422
    private static PlatformSnapshot ReadLegacySnapshot(ConnectivityManager manager)
    {
        var info = manager.ActiveNetworkInfo;
        if (info is not { IsConnected: true })
        {
            return PlatformSnapshot.Empty;
        }

        var transport = info.Type switch
        {
            ConnectivityType.Wifi => NetworkTransport.WiFi,
            ConnectivityType.Mobile => NetworkTransport.Cellular,
            ConnectivityType.Ethernet => NetworkTransport.Ethernet,
            _ => NetworkTransport.Other
        };

        return new PlatformSnapshot(
            IsConnected: true,
            HasNativeInternetCapability: info.IsConnected,
            IsNativeValidated: false,
            IsNativeCaptivePortal: false,
            Transports: [transport],
            IsExpensive: transport == NetworkTransport.Cellular,
            IsConstrained: false,
            InterfaceName: info.TypeName);
    }
#pragma warning restore CS0618, CA1422

    private sealed class Callback : ConnectivityManager.NetworkCallback
    {
        private readonly AndroidNetworkWatcher _owner;

        public Callback(AndroidNetworkWatcher owner) => _owner = owner;

        public override void OnAvailable(Network network) => _owner.OnNetworkEvent();

        public override void OnLost(Network network) => _owner.OnNetworkLost();

        public override void OnUnavailable() => _owner.OnNetworkLost();

        public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities) =>
            _owner.OnNetworkEvent();

        public override void OnLinkPropertiesChanged(Network network, LinkProperties linkProperties) =>
            _owner.OnNetworkEvent();
    }
}
