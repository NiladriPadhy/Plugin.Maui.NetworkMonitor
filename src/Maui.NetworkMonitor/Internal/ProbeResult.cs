namespace Maui.NetworkMonitor.Internal;

internal sealed record ProbeResult(ProbeOutcome Outcome, string? Detail)
{
    public static ProbeResult Unreachable(string? detail = null) =>
        new(ProbeOutcome.Unreachable, detail);

    public static ProbeResult Captive(string? detail = null) =>
        new(ProbeOutcome.CaptivePortal, detail);

    public static ProbeResult Internet(string? detail = null) =>
        new(ProbeOutcome.Internet, detail);
}
