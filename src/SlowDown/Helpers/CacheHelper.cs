using Microsoft.AspNetCore.Http;

namespace Nearform.AspNetCore.SlowDown.Helpers;

internal static class CacheHelper
{
    public static async Task<int> Get(HttpRequest request)
    {
        var ct = GetCancellationToken();
        var opt = SlowDownOptions.CurrentOptions;
        var key = await opt.KeyGenerator(request, ct);

        return await Get(key);
    }

    public static async Task<int> Get(string key)
    {
        var ct = GetCancellationToken();
        var opt = SlowDownOptions.CurrentOptions;
        
        var count = await opt.Cache.GetOrCreateAsync(key, 
            async cancel => await Task.FromResult(0),
            cancellationToken: ct);

        return count;
    }
    
    public static async Task Set(HttpRequest request, int value)
    {
        var ct = GetCancellationToken();
        var opt = SlowDownOptions.CurrentOptions;
        var key = await opt.KeyGenerator(request, ct);

        await Set(key, value);
    }

    public static async Task Set(string key, int value)
    {
        var ct = GetCancellationToken();
        var opt = SlowDownOptions.CurrentOptions;
        
        await opt.Cache.SetAsync(key, value, cancellationToken: ct);
    }
    
    private static CancellationToken GetCancellationToken()
    {
        var timeout = SlowDownOptions.CurrentOptions.CacheTimeout;
        
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(timeout));
        
        return source.Token;
    }
}