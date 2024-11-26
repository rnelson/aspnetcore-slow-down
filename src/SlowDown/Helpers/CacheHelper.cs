using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;

namespace Nearform.AspNetCore.SlowDown.Helpers;

internal static class CacheHelper
{
    public static async Task<(int, int)> Get(HttpRequest request)
    {
        var ct = GetCancellationToken();
        var opt = SlowDownOptions.CurrentOptions;
        var key = await opt.KeyGenerator(request, ct);

        return await Get(key);
    }

    public static async Task<(int, int)> Get(string key)
    {
        var ct = GetCancellationToken();
        var opt = SlowDownOptions.CurrentOptions;

        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMilliseconds(SlowDownOptions.CurrentOptions.TimeWindow),
            LocalCacheExpiration = TimeSpan.FromMilliseconds(SlowDownOptions.CurrentOptions.TimeWindow)
        };

        if (opt.Cache is not null)
        {
            var count = await opt.Cache.GetOrCreateAsync($"{key}_count",
                async _ => await Task.FromResult(0),
                options: cacheOptions,
                cancellationToken: ct);
            var timestamp = await opt.Cache.GetOrCreateAsync($"{key}_timestamp",
                async _ => await Task.FromResult(DateTime.UtcNow.Millisecond),
                options: cacheOptions,
                cancellationToken: ct);

            return (count, timestamp);
        }

        return (0, -1);
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
        {
            await opt.Cache.SetAsync($"{key}_count", value, cancellationToken: ct);

            // Check to see if a timestamp already exists, returning -1 if not
            var timestamp = await opt.Cache.GetOrCreateAsync($"{key}_timestamp",
                async _ => await Task.FromResult(-1),
                options: cacheOptions,
                cancellationToken: ct);

            // Only set the timestamp if it does not yet exist in the cache
            if (timestamp == -1)
                await opt.Cache.SetAsync($"{key}_timestamp", timestamp, cancellationToken: ct);
        }
    }
    
    private static CancellationToken GetCancellationToken()
    {
        var timeout = SlowDownOptions.CurrentOptions.CacheTimeout;
        
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(timeout));
        
        return source.Token;
    }
}