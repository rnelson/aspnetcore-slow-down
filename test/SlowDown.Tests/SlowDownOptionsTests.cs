using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Http;
using Nearform.AspNetCore.SlowDown;

namespace SlowDown.Tests;

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
public class SlowDownOptionsTests
{
    [Fact]
    public void Constructor_Works()
    {
        _ = new SlowDownOptions();
    }
    
    [Fact]
    public void Constructor_HasExpectedDefaults()
    {
        var options = new SlowDownOptions();
        
        Assert.True(options.SlowDownEnabled);
        Assert.Equal(1000, options.Delay);
        Assert.Equal(5, options.DelayAfter);
        Assert.Equal(int.MaxValue, options.MaxDelay);
        Assert.Equal(30000, options.TimeWindow);
        Assert.Null(options.Cache);
        Assert.True(options.AddHeaders);
        Assert.NotNull(options.KeyGenerator);
        Assert.Null(options.OnLimitReached);
        Assert.False(options.SkipFailedRequests);
        Assert.False(options.SkipSuccessfulRequests);
        Assert.Null(options.Skip);
        Assert.Equal(1000, options.CacheTimeout);
    }
    
    [Fact]
    public void Properties_Work()
    {
        var options = new SlowDownOptions
        {
            SlowDownEnabled = true,
            Delay = 1,
            DelayAfter = 1,
            MaxDelay = 1,
            TimeWindow = 1,
            Cache = null!,
            AddHeaders = true,
            OnLimitReached = null,
            SkipFailedRequests = true,
            SkipSuccessfulRequests = true,
            Skip = null,
            CacheTimeout = 1
        };
        
        Assert.True(options.SlowDownEnabled);
        Assert.Equal(1, options.Delay);
        Assert.Equal(1, options.DelayAfter);
        Assert.Equal(1, options.MaxDelay);
        Assert.Equal(1, options.TimeWindow);
        Assert.Null(options.Cache);
        Assert.True(options.AddHeaders);
        Assert.NotNull(options.KeyGenerator);
        Assert.Null(options.OnLimitReached);
        Assert.True(options.SkipFailedRequests);
        Assert.True(options.SkipSuccessfulRequests);
        Assert.Null(options.Skip);
        Assert.Equal(1, options.CacheTimeout);
        
        options = new SlowDownOptions
        {
            SlowDownEnabled = false,
            Delay = 2,
            DelayAfter = 2,
            MaxDelay = 2,
            TimeWindow = 2,
            Cache = null!,
            AddHeaders = false,
            OnLimitReached = null,
            SkipFailedRequests = false,
            SkipSuccessfulRequests = false,
            Skip = null,
            CacheTimeout = 2
        };
        
        Assert.False(options.SlowDownEnabled);
        Assert.Equal(2, options.Delay);
        Assert.Equal(2, options.DelayAfter);
        Assert.Equal(2, options.MaxDelay);
        Assert.Equal(2, options.TimeWindow);
        Assert.Null(options.Cache);
        Assert.False(options.AddHeaders);
        Assert.NotNull(options.KeyGenerator);
        Assert.Null(options.OnLimitReached);
        Assert.False(options.SkipFailedRequests);
        Assert.False(options.SkipSuccessfulRequests);
        Assert.Null(options.Skip);
        Assert.Equal(2, options.CacheTimeout);
    }

    [Fact]
    public void Properties_CurrentOptions_Works()
    {
        const int initial = 5;
        const int expected = 42;

        SlowDownOptions.CurrentOptions = new();
        Assert.Equal(initial, SlowDownOptions.CurrentOptions.DelayAfter);
        
        var newOptions = new SlowDownOptions { DelayAfter = expected };
        SlowDownOptions.CurrentOptions = newOptions;
        
        Assert.Equal(expected, SlowDownOptions.CurrentOptions.DelayAfter);
    }

    [Fact]
    public void Properties_KeyGenerator_Works()
    {
        var options = new SlowDownOptions();
        var keyGenerator1 = UnitTestHelperMethods.CreateKeyGenerator("Hello");
        var keyGenerator2 = UnitTestHelperMethods.CreateKeyGenerator("World");
        
        Assert.NotSame(keyGenerator1, keyGenerator2);
        Assert.NotSame(keyGenerator1, options.KeyGenerator);
        Assert.NotSame(keyGenerator2, options.KeyGenerator);
        
        options.KeyGenerator = keyGenerator1;
        Assert.Same(keyGenerator1, options.KeyGenerator);
        Assert.NotSame(keyGenerator2, options.KeyGenerator);
        
        options.KeyGenerator = keyGenerator2;
        Assert.NotSame(keyGenerator1, options.KeyGenerator);
        Assert.Same(keyGenerator2, options.KeyGenerator);
    }

    [Fact]
    public async Task DefaultKeyGenerator_GetsClientIpWithXForwardedForHeader()
    {
        const string expected = "4.2.2.4";
        var context = new DefaultHttpContext();
        var options = new SlowDownOptions();
        
        context.Request.Headers["X-Forwarded-For"] = expected;
        var key = await options.KeyGenerator(context.Request,
            UnitTestHelperMethods.GetFutureCancellationToken(options));
        
        Assert.Equal(expected, key);
    }

    [Fact]
    public async Task DefaultKeyGenerator_GetsClientIpWithRemoteAddrHeader()
    {
        const string expected = "4.2.2.4";
        var context = new DefaultHttpContext();
        var options = new SlowDownOptions();
        
        context.Request.Headers["REMOTE_ADDR"] = expected;
        var key = await options.KeyGenerator(context.Request,
            UnitTestHelperMethods.GetFutureCancellationToken(options));
        
        Assert.Equal(expected, key);
    }

    [Fact]
    public async Task DefaultKeyGenerator_GetsClientIpWithContextConnectionRemoteIpAddress()
    {
        const string expected = "4.2.2.4";
        var context = new DefaultHttpContext();
        var options = new SlowDownOptions();
        
        context.Connection.RemoteIpAddress = IPAddress.Parse(expected);
        var key = await options.KeyGenerator(context.Request,
            UnitTestHelperMethods.GetFutureCancellationToken(options));
        
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("Hola")]
    [InlineData("el mundo")]
    public async Task Test_HelperMethod_CreateKeyGenerator(string expected)
    {
        var request = UnitTestHelperMethods.CreateHttpContext().Request;
        var cancellationToken = UnitTestHelperMethods.GetCancellationToken();
        
        var generator = UnitTestHelperMethods.CreateKeyGenerator(expected);
        var actual = await generator.Invoke(request, cancellationToken);
        
        Assert.Equal(expected, actual);
    }
}