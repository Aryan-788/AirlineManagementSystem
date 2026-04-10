using Microsoft.AspNetCore.Builder;
using Shared.Middleware;

namespace Shared.Extensions;

public static class CorrelationExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
