using System.Net.Http;

namespace Maui.NetworkMonitor.Internal;

internal sealed class InternetProbe : IDisposable
{
    private readonly NetworkMonitorOptions _options;
    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    public InternetProbe(NetworkMonitorOptions options, HttpMessageHandler? handler = null)
    {
        _options = options;
        if (handler is null)
        {
            _client = new HttpClient(CreateHandler(), disposeHandler: true)
            {
                Timeout = options.ProbeTimeout
            };
            _ownsClient = true;
        }
        else
        {
            _client = new HttpClient(handler, disposeHandler: false)
            {
                Timeout = options.ProbeTimeout
            };
            _ownsClient = true;
        }

        if (!_client.DefaultRequestHeaders.UserAgent.TryParseAdd("Maui.NetworkMonitor/1.0"))
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Maui.NetworkMonitor/1.0");
        }

        _client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };
    }

    public async Task<ProbeResult> ProbeAsync(bool networkIsConnected, CancellationToken cancellationToken)
    {
        if (!_options.EnableHttpProbe)
        {
            return ProbeResult.Unreachable("HTTP probing disabled");
        }

        ProbeResult? captive = null;

        foreach (var endpoint in _options.ProbeEndpoints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await ProbeEndpointAsync(endpoint, networkIsConnected, cancellationToken).ConfigureAwait(false);
            if (result.Outcome == ProbeOutcome.Internet)
            {
                return result;
            }

            if (result.Outcome == ProbeOutcome.CaptivePortal)
            {
                captive ??= result;
            }
        }

        return captive ?? ProbeResult.Unreachable("All probe endpoints failed");
    }

    private async Task<ProbeResult> ProbeEndpointAsync(
        ProbeEndpoint endpoint,
        bool networkIsConnected,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.ProbeTimeout);

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint.Uri);
        request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");

        try
        {
            using var response = await _client.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                timeoutCts.Token).ConfigureAwait(false);

            string? body = null;
            if (endpoint.ExpectedBodyContains is not null || (int)response.StatusCode == 200)
            {
                body = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            }

            var outcome = ProbeClassifier.Classify(
                response.StatusCode,
                body,
                endpoint,
                _options.EnableCaptivePortalDetection);

            return new ProbeResult(outcome, $"{endpoint.Uri.Host} {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProbeResult.Unreachable($"{endpoint.Uri.Host} timed out");
        }
        catch (HttpRequestException ex) when (
            _options.EnableCaptivePortalDetection
            && networkIsConnected
            && ProbeClassifier.LooksLikeTlsInterception(ex))
        {
            return ProbeResult.Captive($"{endpoint.Uri.Host} TLS interception");
        }
        catch (HttpRequestException ex)
        {
            return ProbeResult.Unreachable($"{endpoint.Uri.Host} {ex.GetType().Name}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProbeResult.Unreachable($"{endpoint.Uri.Host} {ex.GetType().Name}");
        }
    }

    private static HttpMessageHandler CreateHandler()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(4),
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            UseCookies = false
        };
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}
