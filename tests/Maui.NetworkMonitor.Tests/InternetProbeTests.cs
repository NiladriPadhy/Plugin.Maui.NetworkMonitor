using System.Net;
using System.Net.Http;
using System.Text;
using Maui.NetworkMonitor.Internal;

namespace Maui.NetworkMonitor.Tests;

public sealed class InternetProbeTests
{
    [Fact]
    public async Task FirstInternetResult_StopsProbing()
    {
        var options = new NetworkMonitorOptions();
        options.ProbeEndpoints.Clear();
        options.ProbeEndpoints.Add(ProbeEndpoint.Generate204("http://one.test/generate_204"));
        options.ProbeEndpoints.Add(ProbeEndpoint.Generate204("http://two.test/generate_204"));

        var handler = new StubHandler(request =>
        {
            if (request.RequestUri!.Host == "one.test")
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html>portal</html>", Encoding.UTF8, "text/html")
            };
        });

        using var probe = new InternetProbe(options, handler);
        var result = await probe.ProbeAsync(networkIsConnected: true, CancellationToken.None);

        Assert.Equal(ProbeOutcome.Internet, result.Outcome);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RedirectThenFailure_IsCaptivePortal()
    {
        var options = new NetworkMonitorOptions();
        options.ProbeEndpoints.Clear();
        options.ProbeEndpoints.Add(ProbeEndpoint.Generate204("http://one.test/generate_204"));
        options.ProbeEndpoints.Add(ProbeEndpoint.Generate204("http://two.test/generate_204"));

        var handler = new StubHandler(request =>
        {
            if (request.RequestUri!.Host == "one.test")
            {
                return new HttpResponseMessage(HttpStatusCode.Redirect)
                {
                    Headers = { Location = new Uri("http://portal.local/login") }
                };
            }

            throw new HttpRequestException("offline");
        });

        using var probe = new InternetProbe(options, handler);
        var result = await probe.ProbeAsync(networkIsConnected: true, CancellationToken.None);

        Assert.Equal(ProbeOutcome.CaptivePortal, result.Outcome);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            _responder = responder;

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_responder(request));
        }
    }
}
