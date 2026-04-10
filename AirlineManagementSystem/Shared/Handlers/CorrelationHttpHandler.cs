using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Handlers;

public class CorrelationHttpHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationHttpHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Get from HttpContext.Items first (set by middleware)
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();

        // 2. Fallback to header if not in items
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = _httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdHeader];
        }

        // 3. Propagate to outgoing request
        if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains(CorrelationIdHeader))
        {
            request.Headers.Add(CorrelationIdHeader, correlationId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
