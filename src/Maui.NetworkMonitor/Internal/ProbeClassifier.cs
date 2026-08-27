using System.Net;

namespace Maui.NetworkMonitor.Internal;

internal static class ProbeClassifier
{
    public static ProbeOutcome Classify(
        HttpStatusCode statusCode,
        string? responseBody,
        ProbeEndpoint endpoint,
        bool enableCaptivePortalDetection)
    {
        var code = (int)statusCode;

        if (enableCaptivePortalDetection && code is >= 300 and < 400)
        {
            return ProbeOutcome.CaptivePortal;
        }

        if (code == endpoint.ExpectedStatusCode)
        {
            if (endpoint.ExpectedStatusCode == 204)
            {
                return ProbeOutcome.Internet;
            }

            if (!string.IsNullOrEmpty(endpoint.ExpectedBodyContains))
            {
                if (!string.IsNullOrEmpty(responseBody) &&
                    responseBody.Contains(endpoint.ExpectedBodyContains, StringComparison.OrdinalIgnoreCase))
                {
                    return ProbeOutcome.Internet;
                }

                return enableCaptivePortalDetection
                    ? ProbeOutcome.CaptivePortal
                    : ProbeOutcome.Unreachable;
            }

            return ProbeOutcome.Internet;
        }

        if (enableCaptivePortalDetection && code == 200)
        {
            // generate_204 (or similar) returned a page instead of the expected empty/204 response.
            return ProbeOutcome.CaptivePortal;
        }

        return ProbeOutcome.Unreachable;
    }

    public static bool LooksLikeTlsInterception(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is System.Security.Authentication.AuthenticationException)
            {
                return true;
            }

            var message = current.Message;
            if (message.Contains("SSL", StringComparison.OrdinalIgnoreCase)
                || message.Contains("TLS", StringComparison.OrdinalIgnoreCase)
                || message.Contains("certificate", StringComparison.OrdinalIgnoreCase)
                || message.Contains("trust", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
