using System.Net;
using Microsoft.AspNetCore.Http;
using Nearform.AspNetCore.SlowDown;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace SlowDown.Tests.Helpers;

public class AspNetCoreHelperTests
{
    [Fact]
    public async Task GetClientIp_ReturnsEmptyWhenCancelled()
    {
        var cancellationTokenSource = UnitTestHelperMethods.CreateCancelledCancellationTokenSource();
        var context = new DefaultHttpContext();
        
        var clientIp = await AspNetCoreHelper.GetClientIp(context.Request, cancellationTokenSource.Token);
        Assert.Equal(string.Empty, clientIp);
    }

    [Fact]
    public async Task GetClientIp_ReturnsValueFromContextConnectionRemoteIpAddress()
    {
        const string expected = "4.2.2.4";
        var context = new DefaultHttpContext();
        var options = new SlowDownOptions();
        var cancellationToken = UnitTestHelperMethods.CreateCancellationToken();
        
        context.Connection.RemoteIpAddress = IPAddress.Parse(expected);
        var clientIp = await options.KeyGenerator(context.Request, cancellationToken);
        
        Assert.Equal(expected, clientIp);
    }
    
    [Fact]
    public async Task GetClientIp_ReturnsValueFromRemoteAddrHeader()
    {
        const string expected = "4.2.2.4";
        var context = new DefaultHttpContext();
        var options = new SlowDownOptions();
        var cancellationToken = UnitTestHelperMethods.CreateCancellationToken();
        
        context.Request.Headers["REMOTE_ADDR"] = expected;
        var clientIp = await options.KeyGenerator(context.Request, cancellationToken);
        
        Assert.Equal(expected, clientIp);
    }
    
    [Fact]
    public async Task GetClientIp_ReturnsValueFromXForwardedForHeader()
    {
        const string expected = "4.2.2.4";
        var context = new DefaultHttpContext();
        var options = new SlowDownOptions();
        var cancellationToken = UnitTestHelperMethods.CreateCancellationToken();
        
        context.Request.Headers["X-Forwarded-For"] = expected;
        var clientIp = await options.KeyGenerator(context.Request, cancellationToken);
        
        Assert.Equal(expected, clientIp);
    }
    
    [Fact]
    public async Task GetClientIp_ThrowsExceptionWhenExpected()
    {
        var context = new DefaultHttpContext();
        var options = new SlowDownOptions();
        var cancellationToken = UnitTestHelperMethods.CreateCancellationToken();
        
        await Assert.ThrowsAsync<HttpProtocolException>(async () => await options.KeyGenerator(context.Request, cancellationToken));
    }
}