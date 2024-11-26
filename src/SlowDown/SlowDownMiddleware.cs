using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace Nearform.AspNetCore.SlowDown;

public class SlowDownMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SlowDownMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public SlowDownMiddleware(RequestDelegate next, ILogger<SlowDownMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // We require HybridCache for caching. Make sure the user added it.
        SlowDownOptions.CurrentOptions.Cache ??= 
            (_serviceProvider.GetRequiredService(typeof(HybridCache)) as HybridCache)!;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (SlowDownOptions.CurrentOptions.SlowDownEnabled)
            {
                await HandleSlowDown(context);
                await HandleSkipConditions(context);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "an error occurred while processing request: {Message}", e.Message);
        }
        finally
        {
            await _next(context);
        }
    }

    private static async Task HandleSkipConditions(HttpContext context)
    {
        var opt = SlowDownOptions.CurrentOptions;
        var ip = await AspNetCoreHelper.GetClientIp(context.Request);
        
        var shouldSkip = SlowDownOptions.CurrentOptions.Skip?.Invoke(context.Request) ?? false;

        if ((opt.SkipFailedRequests && context.Response.StatusCode >= 400) ||
            (opt.SkipSuccessfulRequests && context.Response.StatusCode < 400) ||
            shouldSkip)
        {
            await ChangeCount(ip, -1);

            if (opt.AddHeaders)
            {
                var delay = int.Parse(context.Response.Headers[Constants.DelayHeader].ToString());
                var remaining = int.Parse(context.Response.Headers[Constants.DelayHeader].ToString());
                AddHeaders(context, delay, opt.DelayAfter, remaining + 1);
            }
        }
    }

    private static async Task HandleSlowDown(HttpContext context)
    {
        var opt = SlowDownOptions.CurrentOptions;
        var ip = await AspNetCoreHelper.GetClientIp(context.Request);
        var (newCount, _) = await ChangeCount(ip);

        var delay = CalculateDelay(newCount);
        var remaining = Math.Max(opt.DelayAfter - newCount, 0);
        
        if (opt.AddHeaders)
            AddHeaders(context, delay, opt.DelayAfter, remaining);

        if (delay > 0)
        {
            SlowDownOptions.CurrentOptions.OnLimitReached?.Invoke(context.Request);
            
            if (!opt.FakeDelay)
                await Task.Delay(delay);
        }
    }

    private static void AddHeaders(HttpContext context, int delay, int delayAfter, int remaining)
    {
        context.Response.Headers[Constants.DelayHeader] = delay.ToString();
        context.Response.Headers[Constants.LimitHeader] = delayAfter.ToString();
        context.Response.Headers[Constants.RemainingHeader] = remaining.ToString();
    }

    private static async Task<(int, int)> ChangeCount(string ip, int delta = 1)
    {
        var now = DateTime.UtcNow.Millisecond;
        var (currentCount, addedTimestamp) = await CacheHelper.Get(ip);
        
        var newCount = currentCount + delta;
        var ttl = SlowDownOptions.CurrentOptions.TimeWindow - now - addedTimestamp;
        
        await CacheHelper.Set(ip, newCount);
        
        return (newCount, ttl);
    }

    private static int CalculateDelay(int requestCount)
    {
        var opt = SlowDownOptions.CurrentOptions;

        if (opt.DelayAfter == 0 || opt.Delay == 0 || opt.TimeWindow == 0 || opt.MaxDelay == 0)
            return 0;

        if (requestCount <= opt.DelayAfter)
            return 0;

        var remaining = Math.Max(requestCount - opt.DelayAfter, 0);
        return Math.Min(remaining * opt.Delay, opt.MaxDelay);
    }
}