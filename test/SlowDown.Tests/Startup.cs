using System.Diagnostics.CodeAnalysis;
using Libexec.AspNetCore.SlowDown;
using Libexec.AspNetCore.SlowDown.Helpers;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

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