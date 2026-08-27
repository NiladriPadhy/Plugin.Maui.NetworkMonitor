namespace Maui.NetworkMonitor.Internal;

internal sealed record PlatformSnapshot(
    bool IsConnected,
    bool HasNativeInternetCapability,
    bool IsNativeValidated,
    bool IsNativeCaptivePortal,
    IReadOnlyList<NetworkTransport> Transports,
    bool IsExpensive,
    bool IsConstrained,
    string? InterfaceName)
{
    public static PlatformSnapshot Empty { get; } = new(
        IsConnected: false,
        HasNativeInternetCapability: false,
        IsNativeValidated: false,
        IsNativeCaptivePortal: false,
        Transports: [NetworkTransport.None],
        IsExpensive: false,
        IsConstrained: false,
        InterfaceName: null);
}
