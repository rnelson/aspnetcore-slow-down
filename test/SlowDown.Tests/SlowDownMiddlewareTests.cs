namespace SlowDown.Tests;

public class SlowDownMiddlewareTests
{
    [Fact]
    public void Constructor_Works()
    {
        _ = UnitTestHelperMethods.CreateSlowDownMiddleware();
    }
    
    [Fact]
    public async Task InvokeAsync_WorksDefault()
    {
        var context = UnitTestHelperMethods.CreateHttpContext();
        var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

        await middleware.InvokeAsync(context);
    }
    
    [Fact]
    public async Task InvokeAsync_WorksWithSlowDownDisabled()
    {
        var context = UnitTestHelperMethods.CreateHttpContext();
        var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

        await middleware.InvokeAsync(context);
    }
}
