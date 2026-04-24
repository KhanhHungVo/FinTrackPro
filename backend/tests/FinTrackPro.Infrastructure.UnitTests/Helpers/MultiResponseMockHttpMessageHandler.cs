using System.Net;
using System.Text;

namespace FinTrackPro.Infrastructure.UnitTests.Helpers;

/// <summary>
/// Returns responses from the provided queue in order. The last response is repeated
/// if more requests arrive than responses queued.
/// </summary>
public class MultiResponseMockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Json)> _responses;

    public MultiResponseMockHttpMessageHandler(IEnumerable<(HttpStatusCode, string)> responses)
    {
        _responses = new Queue<(HttpStatusCode, string)>(responses);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (status, json) = _responses.Count > 1
            ? _responses.Dequeue()
            : _responses.Peek();
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }
}
