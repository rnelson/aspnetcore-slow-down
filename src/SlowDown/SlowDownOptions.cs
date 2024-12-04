using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace Nearform.AspNetCore.SlowDown;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public sealed class SlowDownOptions
{
    /// <summary>
    /// Flag indicating that the delay should not actually be applied.
    /// </summary>
    /// <remarks>
    /// This is only intended for use with unit tests.
    /// </remarks>
    internal bool FakeDelay { get; set; }
    
    /// <summary>
    /// Flag to enable or disable the middleware.
    /// </summary>
    public bool SlowDownEnabled { get; set; } = true;
    
    /// <summary>
    /// Base unit of time delay applied to requests, in milliseconds. Set
    /// to <c>0</c> to disable delaying.
    /// </summary>
    public int Delay { get; set; } = 1000;
    
    /// <summary>
    /// Number of requests received during <see cref="TimeWindow"/> before
    /// starting to delay responses. Set to <c>0</c> to disable delaying.
    /// </summary>
    public int DelayAfter { get; set; } = 5;
    
    /// <summary>
    /// The maximum value of delay that a request has after many consecutive
    /// attempts. It is an important option for the server when it is running
    /// behind a load balancer or reverse proxy, and has a request timeout. Set
    /// to <c>0</c> to disable delaying.
    /// </summary>
    public int MaxDelay { get; set; } = int.MaxValue;

    /// <summary>
    /// The duration, in milliseconds, of the time window during which request
    /// counts are kept in memory. Set to <c>0</c> to disable delaying.
    /// </summary>
    public int TimeWindow { get; set; } = 30000;
    
    /// <summary>
    /// A <see cref="HybridCache"/> instance to use for caching clients and
    /// their request counts.
    /// </summary>
    public HybridCache? Cache { get; set; }

    /// <summary>
    /// Flag to add custom headers (<c>X-Slow-Down-Limit</c>, <c>X-Slow-Down-Remaining</c>,
    /// <c>X-Slow-Down-Delay</c>) for all server responses.
    /// </summary>
    public bool AddHeaders { get; set; } = true;

    /// <summary>
    /// Function used to generate keys to uniquely identify requests coming
    /// from the same user.
    /// </summary>
    public Func<HttpRequest, CancellationToken, Task<string>> KeyGenerator { get; set; } = AspNetCoreHelper.GetClientIp;

    /// <summary>
    /// Function that gets called the first time the limit is reached within
    /// <see cref="TimeWindow"/>.
    /// </summary>
    public Action<HttpRequest>? OnLimitReached { get; set; }

    /// <summary>
    /// When <c>true</c>, failed requests (status &gt;= 400) won't be counted.
    /// </summary>
    /// <remarks>
    /// This only has an effect with requests that have failed before this middleware
    /// runs. Any middleware or controllers that run after this may mark the request
    /// as having failed, but it will still count towards client requests here. 
    /// </remarks>
    public bool SkipFailedRequests { get; set; }

    /// <summary>
    /// When <c>true</c>, failed requests (status &lt; 400) won't be counted.
    /// </summary>
    /// <remarks>
    /// This only has an effect with requests that have succeeded before this middleware
    /// runs. Any middleware or controllers that run after this may mark the request
    /// as having failed, but it will still count towards client requests here. 
    /// </remarks>
    public bool SkipSuccessfulRequests { get; set; }

    /// <summary>
    /// Function used to skip requests. Returning <c>true</c> from the function will
    /// skip limiting for that request.
    /// </summary>
    public Func<HttpRequest, bool>? Skip { get; set; }
    
    /// <summary>
    /// Time limit, in ms, for the cache lookup to run. If the time limit is exceeded,
    /// the middleware will stop processing the request and allow it to proceed normally.
    /// </summary>
    public int CacheTimeout { get; set; } = 5000;

    /// <summary>
    /// Current configuration instance used by the <see cref="SlowDownMiddleware"/>.
    /// </summary>
    internal static SlowDownOptions CurrentOptions { get; set; } = new();
}