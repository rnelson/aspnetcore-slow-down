using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace Nearform.AspNetCore.SlowDown.Helpers;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation")]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
public class CacheHelper(SlowDownOptions options, IDistributedCache distributedCache)
{
    private readonly SlowDownOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IDistributedCache _cache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
    
    public async Task<int> Get(HttpRequest request)
    {
        var ct = GetCancellationToken();
        var key = await _options.KeyGenerator(request, ct);

        return await Get(key);
    }

    public async Task<int> Get(string key)
    {
        var ct = GetCancellationToken();

        if (int.TryParse(await _cache.GetStringAsync($"{key}_count", ct), out var count))
            return count;
        
        count = 0;
        await _cache.SetStringAsync($"{key}_count", count.ToString(), token: ct);
        
        return count;
    }
    
    public async Task Set(HttpRequest request, int value)
    {
        var ct = GetCancellationToken();
        var key = await _options.KeyGenerator(request, ct);

        await Set(key, value);
    }

    public async Task Set(string key, int value)
    {
        var ct = GetCancellationToken();
        
        await _cache.SetStringAsync($"{key}_count", value.ToString(), token: ct);
    }
    
    private CancellationToken GetCancellationToken()
    {
        var timeout = _options.CacheTimeout;
        
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(timeout));
        
        return source.Token;
    }
}