﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;

namespace Nearform.AspNetCore.SlowDown.Helpers;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation")]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
public class CacheHelper(SlowDownOptions options, HybridCache cache)
{
    private readonly SlowDownOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly HybridCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    
    public async Task<int> Get(HttpRequest request, IEnumerable<string>? tags = null)
    {
        var ct = GetCancellationToken();
        var key = await _options.KeyGenerator(request, ct);

        return await Get(key, tags);
    }

    public async Task<int> Get(string key, IEnumerable<string>? tags = null)
    {
        var ct = GetCancellationToken();
        
        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMilliseconds(_options.TimeWindow),
            LocalCacheExpiration = TimeSpan.FromMilliseconds(_options.TimeWindow),
            //Flags = HybridCacheEntryFlags.DisableLocalCache
        };
        
        var count = await _cache.GetOrCreateAsync($"{key}_count",
            async _ => await Task.FromResult(0),
            options: cacheOptions,
            cancellationToken: ct,
            tags: tags);

        return count;
    }

    public async Task Remove(HttpRequest request)
    {
        var ct = GetCancellationToken();
        var key = await _options.KeyGenerator(request, ct);
        
        await _cache.RemoveAsync(key, cancellationToken: ct);
    }

    public async Task Remove(string key)
    {
        var ct = GetCancellationToken();
        await _cache.RemoveAsync(key, cancellationToken: ct);
    }

    public async Task RemoveAll(IEnumerable<string> tags)
    {
        var ct = GetCancellationToken();
        await _cache.RemoveByTagAsync(tags, cancellationToken: ct);
    }
    
    public async Task Set(HttpRequest request, int value, IEnumerable<string>? tags = null)
    {
        var ct = GetCancellationToken();
        var key = await _options.KeyGenerator(request, ct);

        await Set(key, value, tags);
    }

    public async Task Set(string key, int value, IEnumerable<string>? tags = null)
    {
        var ct = GetCancellationToken();

        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMilliseconds(_options.TimeWindow),
            LocalCacheExpiration = TimeSpan.FromMilliseconds(_options.TimeWindow),
            //Flags = HybridCacheEntryFlags.DisableLocalCache
        };

        await _cache.SetAsync($"{key}_count", value, options: cacheOptions, cancellationToken: ct, tags: tags);
    }
    
    private CancellationToken GetCancellationToken()
    {
        var timeout = _options.CacheTimeout;
        
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(timeout));
        
        return source.Token;
    }
}