using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Nearform.AspNetCore.SlowDown;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace SlowDown.Tests;

public class Startup
{
    [Experimental("EXTEXP0018")]
    protected void ConfigureServices(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();

        services.AddHybridCache();
        
        var options = new SlowDownOptions();
        services.AddSingleton(options);
        
        var cacheHelper = new CacheHelper(options, provider.GetRequiredService<IDistributedCache>());
        services.AddSingleton(cacheHelper);
    }
}