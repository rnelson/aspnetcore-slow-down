using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Nearform.AspNetCore.SlowDown;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace SlowDown.Tests;

public class Startup
{
    [Experimental("EXTEXP0018")]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHybridCache();
    
        var options = new SlowDownOptions();
        services.AddSingleton(options);
    
        var provider = services.BuildServiceProvider();
        var cacheHelper = new CacheHelper(options, provider.GetRequiredService<HybridCache>());
        services.AddSingleton(cacheHelper);
    }
}