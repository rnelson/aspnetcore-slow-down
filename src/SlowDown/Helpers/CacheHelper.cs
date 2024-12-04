using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;

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

        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMilliseconds(SlowDownOptions.CurrentOptions.TimeWindow),
            LocalCacheExpiration = TimeSpan.FromMilliseconds(SlowDownOptions.CurrentOptions.TimeWindow)
        };

        if (opt.Cache is null)
            return 0;
        
        var count = await opt.Cache.GetOrCreateAsync($"{key}_count",
            async _ => await Task.FromResult(0),
            options: cacheOptions,
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

        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMilliseconds(SlowDownOptions.CurrentOptions.TimeWindow),
            LocalCacheExpiration = TimeSpan.FromMilliseconds(SlowDownOptions.CurrentOptions.TimeWindow)
        };

        if (opt.Cache is not null)
            await opt.Cache.SetAsync($"{key}_count", value, options: cacheOptions, cancellationToken: ct);
    }
    
    private static CancellationToken GetCancellationToken()
    {
        var timeout = SlowDownOptions.CurrentOptions.CacheTimeout;
        
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(timeout));
        
        return source.Token;
    }
}