using System.Net;
using System.Text;

namespace FinTrackPro.Infrastructure.UnitTests.Helpers;

public class MockHttpMessageHandler(HttpStatusCode status, string json) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }
}
