using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Nearform.AspNetCore.SlowDown.Helpers;

internal static class AspNetCoreHelper
{
    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global", Justification = "Allowing overriding methods")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static Task<string> GetClientIp(HttpRequest request, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(string.Empty);
        
        var headers = request.Headers;
        var connection = request.HttpContext.Connection;

        if (headers.TryGetValue("X-Forwarded-For", out var xForwardedForHeader))
            return Task.FromResult(xForwardedForHeader.ToString());

        if (headers.TryGetValue("REMOTE_ADDR", out var remoteAddrHeader))
            return Task.FromResult(remoteAddrHeader.ToString());

        if (!string.IsNullOrWhiteSpace(connection.RemoteIpAddress?.ToString()))
            return Task.FromResult(connection.RemoteIpAddress!.ToString());

        throw new HttpProtocolException(500, "unable to get client ip", null);
    }
}