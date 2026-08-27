namespace Maui.NetworkMonitor;

/// <summary>
/// An HTTP endpoint used to distinguish real internet access from a captive portal.
/// </summary>
public sealed class ProbeEndpoint
{
    /// <summary>
    /// Creates a probe endpoint.
    /// </summary>
    /// <param name="url">Absolute HTTP or HTTPS URL.</param>
    /// <param name="expectedStatusCode">Status code that indicates a clean (non-portal) response.</param>
    /// <param name="expectedBodyContains">Optional substring that must appear in a 200 body.</param>
    public ProbeEndpoint(string url, int expectedStatusCode = 204, string? expectedBodyContains = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Probe URL must be an absolute http or https URI.", nameof(url));
        }

        if (expectedStatusCode is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedStatusCode));
        }

        Uri = uri;
        ExpectedStatusCode = expectedStatusCode;
        ExpectedBodyContains = expectedBodyContains;
    }

    /// <summary>Absolute URL that is fetched during a probe.</summary>
    public Uri Uri { get; }

    /// <summary>Status code expected when the path is not intercepted.</summary>
    public int ExpectedStatusCode { get; }

    /// <summary>Optional body fragment required for a successful 200 response.</summary>
    public string? ExpectedBodyContains { get; }

    /// <summary>Creates a generate_204-style probe that expects HTTP 204 and an empty body.</summary>
    public static ProbeEndpoint Generate204(string url) => new(url, 204);

    /// <summary>Creates a probe that expects HTTP 200 and a known success fragment in the body.</summary>
    public static ProbeEndpoint SuccessContent(string url, string expectedBodyContains) =>
        new(url, 200, expectedBodyContains);
}
