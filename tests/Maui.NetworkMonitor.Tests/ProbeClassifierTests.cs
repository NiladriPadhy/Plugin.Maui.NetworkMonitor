using System.Net;
using Maui.NetworkMonitor.Internal;

namespace Maui.NetworkMonitor.Tests;

public sealed class ProbeClassifierTests
{
    private static readonly ProbeEndpoint Generate204 =
        ProbeEndpoint.Generate204("http://connectivitycheck.gstatic.com/generate_204");

    private static readonly ProbeEndpoint AppleSuccess =
        ProbeEndpoint.SuccessContent("http://captive.apple.com/hotspot-detect.html", "Success");

    [Fact]
    public void Generate204_NoContent_IsInternet()
    {
        var outcome = ProbeClassifier.Classify(HttpStatusCode.NoContent, null, Generate204, true);
        Assert.Equal(ProbeOutcome.Internet, outcome);
    }

    [Fact]
    public void Generate204_Redirect_IsCaptivePortal()
    {
        var outcome = ProbeClassifier.Classify(HttpStatusCode.Redirect, "<html>login</html>", Generate204, true);
        Assert.Equal(ProbeOutcome.CaptivePortal, outcome);
    }

    [Fact]
    public void Generate204_UnexpectedOk_IsCaptivePortal()
    {
        var outcome = ProbeClassifier.Classify(HttpStatusCode.OK, "<html>hotel wifi</html>", Generate204, true);
        Assert.Equal(ProbeOutcome.CaptivePortal, outcome);
    }

    [Fact]
    public void SuccessBody_MatchingContent_IsInternet()
    {
        var outcome = ProbeClassifier.Classify(
            HttpStatusCode.OK,
            "<HTML><HEAD><TITLE>Success</TITLE></HEAD><BODY>Success</BODY></HTML>",
            AppleSuccess,
            true);

        Assert.Equal(ProbeOutcome.Internet, outcome);
    }

    [Fact]
    public void SuccessBody_WrongContent_IsCaptivePortal()
    {
        var outcome = ProbeClassifier.Classify(
            HttpStatusCode.OK,
            "<html><body>Sign in to the network</body></html>",
            AppleSuccess,
            true);

        Assert.Equal(ProbeOutcome.CaptivePortal, outcome);
    }

    [Fact]
    public void ServerError_IsUnreachable()
    {
        var outcome = ProbeClassifier.Classify(HttpStatusCode.BadGateway, null, Generate204, true);
        Assert.Equal(ProbeOutcome.Unreachable, outcome);
    }

    [Theory]
    [InlineData("The SSL connection could not be established")]
    [InlineData("certificate is not trusted")]
    [InlineData("TLS handshake failed")]
    public void TlsLanguage_IsInterception(string message)
    {
        Assert.True(ProbeClassifier.LooksLikeTlsInterception(new HttpRequestException(message)));
    }

    [Fact]
    public void AuthenticationException_IsInterception()
    {
        var exception = new HttpRequestException(
            "auth",
            new System.Security.Authentication.AuthenticationException("untrusted"));

        Assert.True(ProbeClassifier.LooksLikeTlsInterception(exception));
    }
}
