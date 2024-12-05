using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace Nearform.AspNetCore.SlowDown;

[SuppressMessage("ReSharper", "HeapView.ObjectAllocation")]
public class SlowDownMiddleware(
    RequestDelegate next,
    ILogger<SlowDownMiddleware> logger,
    SlowDownOptions options,
    CacheHelper cacheHelper)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<SlowDownMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SlowDownOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly CacheHelper _cacheHelper = cacheHelper ?? throw new ArgumentNullException(nameof(cacheHelper));

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (_options.SlowDownEnabled)
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

    private async Task HandleSkipConditions(HttpContext context)
    {
        var ip = await AspNetCoreHelper.GetClientIp(context.Request);
        
        var shouldSkip = _options.Skip?.Invoke(context.Request) ?? false;

        if ((_options.SkipFailedRequests && context.Response.StatusCode >= 400) ||
            (_options.SkipSuccessfulRequests && context.Response.StatusCode < 400) ||
            shouldSkip)
        {
            await ChangeCount(ip, -1);

            if (_options.AddHeaders)
            {
                var delay = int.Parse(context.Response.Headers[Constants.DelayHeader].ToString());
                var remaining = int.Parse(context.Response.Headers[Constants.DelayHeader].ToString());
                AddHeaders(context, delay, _options.DelayAfter, remaining + 1);
            }
        }
    }

    private async Task HandleSlowDown(HttpContext context)
    {
        var ip = await AspNetCoreHelper.GetClientIp(context.Request);
        var newCount = await ChangeCount(ip);

        var delay = CalculateDelay(newCount);
        var remaining = Math.Max(_options.DelayAfter - newCount, 0);
        
        if (_options.AddHeaders)
            AddHeaders(context, delay, _options.DelayAfter, remaining);

        if (delay > 0)
        {
            _options.OnLimitReached?.Invoke(context.Request);
            
            if (!_options.FakeDelay)
                await Task.Delay(delay);
        }
    }

    private static void AddHeaders(HttpContext context, int delay, int delayAfter, int remaining)
    {
        context.Response.Headers[Constants.DelayHeader] = delay.ToString();
        context.Response.Headers[Constants.LimitHeader] = delayAfter.ToString();
        context.Response.Headers[Constants.RemainingHeader] = remaining.ToString();
    }

    private async Task<int> ChangeCount(string ip, int delta = 1)
    {
        var currentCount = await _cacheHelper.Get(ip);
        var newCount = currentCount + delta;
        
        await _cacheHelper.Set(ip, newCount);
        
        return newCount;
    }

    private int CalculateDelay(int requestCount)
    {
        if (_options.DelayAfter == 0 || _options.Delay == 0 || _options.TimeWindow == 0 || _options.MaxDelay == 0)
            return 0;

        if (requestCount <= _options.DelayAfter)
            return 0;

        var remaining = Math.Max(requestCount - _options.DelayAfter, 0);
        return Math.Min(remaining * _options.Delay, _options.MaxDelay);
    }
}