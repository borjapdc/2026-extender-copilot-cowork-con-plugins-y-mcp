using System.Net.Http.Headers;

namespace SanctuaryIntelligence.Mcp.Auth;

public sealed class ForwardBearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var header)
            && string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = header;
        }

        return base.SendAsync(request, cancellationToken);
    }
}