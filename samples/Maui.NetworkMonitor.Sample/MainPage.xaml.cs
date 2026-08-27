using System.Collections.ObjectModel;

namespace Maui.NetworkMonitor.Sample;

public partial class MainPage : ContentPage
{
    private readonly INetworkMonitor _monitor;
    private readonly ObservableCollection<string> _events = [];

    public MainPage()
    {
        InitializeComponent();
        _monitor = IPlatformApplication.Current?.Services.GetRequiredService<INetworkMonitor>()
            ?? throw new InvalidOperationException("INetworkMonitor is not registered.");
        EventList.ItemsSource = _events;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _monitor.StatusChanged += OnStatusChanged;
        Render(_monitor.Current);
    }

    protected override void OnDisappearing()
    {
        _monitor.StatusChanged -= OnStatusChanged;
        base.OnDisappearing();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        RefreshButton.IsEnabled = false;
        try
        {
            var status = await _monitor.RefreshAsync();
            Render(status);
            Prepend($"Manual refresh · {status}");
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void OnStatusChanged(object? sender, NetworkStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Render(e.Current);
            var transition = e.IsTransportTransition
                ? $" {e.Previous.PrimaryTransport} → {e.Current.PrimaryTransport}"
                : string.Empty;
            Prepend($"{e.ChangeKind}{transition} · {e.Current.Reachability}");
        });
    }

    private void Render(NetworkStatus status)
    {
        ReachabilityLabel.Text = status.Reachability switch
        {
            InternetReachability.Internet => "Internet",
            InternetReachability.CaptivePortal => "Captive portal",
            InternetReachability.LocalNetworkOnly => "Local only",
            InternetReachability.Offline => "Offline",
            _ => "Checking…"
        };

        HeroCard.BackgroundColor = status.Reachability switch
        {
            InternetReachability.Internet => Color.FromArgb("#14532D"),
            InternetReachability.CaptivePortal => Color.FromArgb("#78350F"),
            InternetReachability.LocalNetworkOnly => Color.FromArgb("#1E3A5F"),
            InternetReachability.Offline => Color.FromArgb("#7F1D1D"),
            _ => Color.FromArgb("#12203A")
        };

        SummaryLabel.Text = status.Reachability == InternetReachability.Unknown
            ? "Waiting for the first path + probe"
            : $"{status.PrimaryTransport} · internet={status.HasInternet} · {status.InterfaceName ?? "no iface"}";

        TransportLabel.Text = status.ActiveTransports.Count == 0
            ? status.PrimaryTransport.ToString()
            : string.Join(" + ", status.ActiveTransports);
        CaptiveLabel.Text = status.IsCaptivePortal ? "Yes" : "No";
        ExpensiveLabel.Text = status.IsExpensive ? "Yes" : "No";
        ConstrainedLabel.Text = status.IsConstrained ? "Yes" : "No";
    }

    private void Prepend(string line)
    {
        _events.Insert(0, $"{DateTime.Now:HH:mm:ss}  {line}");
        while (_events.Count > 80)
        {
            _events.RemoveAt(_events.Count - 1);
        }
    }
}
