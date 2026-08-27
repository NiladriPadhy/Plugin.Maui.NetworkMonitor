using Maui.NetworkMonitor.Internal;

namespace Maui.NetworkMonitor;

/// <summary>
/// Default <see cref="INetworkMonitor"/> that combines native path events with HTTP probes.
/// </summary>
public sealed class NetworkMonitor : INetworkMonitor
{
    private readonly NetworkMonitorOptions _options;
    private readonly IPlatformNetworkWatcher _watcher;
    private readonly InternetProbe _probe;
    private readonly object _gate = new();
    private readonly bool _ownsDependencies;

    private SynchronizationContext? _synchronizationContext;
    private CancellationTokenSource? _monitorCts;
    private CancellationTokenSource? _refreshCts;
    private PeriodicTimer? _reprobeTimer;
    private Task? _reprobeLoop;
    private NetworkStatus _current = NetworkStatus.Unknown;
    private bool _disposed;

    /// <summary>Creates a monitor with default options.</summary>
    public NetworkMonitor()
        : this(new NetworkMonitorOptions())
    {
    }

    /// <summary>Creates a monitor with the supplied options.</summary>
    public NetworkMonitor(NetworkMonitorOptions options)
        : this(options, PlatformNetworkWatcherFactory.Create(), new InternetProbe(options), ownsDependencies: true)
    {
    }

    internal NetworkMonitor(
        NetworkMonitorOptions options,
        IPlatformNetworkWatcher watcher,
        InternetProbe probe,
        bool ownsDependencies = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(watcher);
        ArgumentNullException.ThrowIfNull(probe);

        options.Validate();
        _options = options;
        _watcher = watcher;
        _probe = probe;
        _ownsDependencies = ownsDependencies;
    }

    /// <inheritdoc />
    public NetworkStatus Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc />
    public bool IsMonitoring
    {
        get
        {
            lock (_gate)
            {
                return _monitorCts is { IsCancellationRequested: false };
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<NetworkStatusChangedEventArgs>? StatusChanged;

    /// <summary>Creates a monitor and optionally configures it.</summary>
    public static NetworkMonitor Create(Action<NetworkMonitorOptions>? configure = null)
    {
        var options = new NetworkMonitorOptions();
        configure?.Invoke(options);
        return new NetworkMonitor(options);
    }

    /// <inheritdoc />
    public void Start()
    {
        ThrowIfDisposed();

        lock (_gate)
        {
            if (_monitorCts is { IsCancellationRequested: false })
            {
                return;
            }

            _synchronizationContext = SynchronizationContext.Current;
            _monitorCts = new CancellationTokenSource();
            _watcher.Changed += OnPlatformChanged;
            _watcher.Start();

            if (_options.ReprobeInterval > TimeSpan.Zero)
            {
                _reprobeTimer = new PeriodicTimer(_options.ReprobeInterval);
                _reprobeLoop = RunReprobeLoopAsync(_reprobeTimer, _monitorCts.Token);
            }
        }

        _ = RefreshCoreAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    public void Stop()
    {
        CancellationTokenSource? monitorCts;
        PeriodicTimer? timer;

        lock (_gate)
        {
            monitorCts = _monitorCts;
            _monitorCts = null;
            timer = _reprobeTimer;
            _reprobeTimer = null;
            _reprobeLoop = null;
            _watcher.Changed -= OnPlatformChanged;
            _watcher.Stop();
        }

        try
        {
            monitorCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        timer?.Dispose();
        monitorCts?.Dispose();
        CancelRefresh();
    }

    /// <inheritdoc />
    public Task<NetworkStatus> RefreshAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return RefreshCoreAsync(cancellationToken);
    }

    private void OnPlatformChanged(object? sender, PlatformSnapshot snapshot)
    {
        var debounce = _options.EventDebounce;
        if (debounce <= TimeSpan.Zero)
        {
            _ = RefreshCoreAsync(CancellationToken.None);
            return;
        }

        var cts = new CancellationTokenSource();
        CancellationTokenSource? previous;
        lock (_gate)
        {
            previous = _refreshCts;
            _refreshCts = cts;
        }

        previous?.Cancel();
        previous?.Dispose();

        _ = DebouncedRefreshAsync(cts, debounce);
    }

    private async Task DebouncedRefreshAsync(CancellationTokenSource cts, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, cts.Token).ConfigureAwait(false);
            await RefreshCoreAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunReprobeLoopAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<NetworkStatus> RefreshCoreAsync(CancellationToken cancellationToken)
    {
        var snapshot = _watcher.Current;
        ProbeResult? probe = null;

        if (_options.EnableHttpProbe && snapshot.IsConnected)
        {
            try
            {
                probe = await _probe.ProbeAsync(snapshot.IsConnected, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                probe = ProbeResult.Unreachable("probe failed");
            }
        }

        var next = StatusComposer.Compose(snapshot, probe);
        Publish(next);
        return next;
    }

    private void Publish(NetworkStatus next)
    {
        NetworkStatus previous;
        lock (_gate)
        {
            previous = _current;
            if (previous.IsEquivalentTo(next))
            {
                _current = next;
                return;
            }

            _current = next;
        }

        var changeKind = ChangeKindDetector.Detect(previous, next);
        var args = new NetworkStatusChangedEventArgs(previous, next, changeKind);
        RaiseStatusChanged(args);
    }

    private void RaiseStatusChanged(NetworkStatusChangedEventArgs args)
    {
        var handler = StatusChanged;
        if (handler is null)
        {
            return;
        }

        if (_options.RaiseEventsOnCapturedContext && _synchronizationContext is { } context
            && SynchronizationContext.Current != context)
        {
            context.Post(_ => handler(this, args), null);
            return;
        }

        handler(this, args);
    }

    private void CancelRefresh()
    {
        CancellationTokenSource? refreshCts;
        lock (_gate)
        {
            refreshCts = _refreshCts;
            _refreshCts = null;
        }

        try
        {
            refreshCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        refreshCts?.Dispose();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();

        if (_ownsDependencies)
        {
            _probe.Dispose();
            _watcher.Dispose();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
