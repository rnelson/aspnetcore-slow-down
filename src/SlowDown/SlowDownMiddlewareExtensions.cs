using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace Nearform.AspNetCore.SlowDown;

[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
public static class SlowDownMiddlewareExtensions
{
    /// <summary>
    /// Configure the Slow Down middleware in Startup.ConfigureServices().
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configAction">Configuration action.</param>
    /// <returns></returns>
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident", Justification = "Initializing the options object")]
    public static IServiceCollection AddSlowDown(this IServiceCollection services,
        Action<SlowDownOptions>? configAction = null)
    {
        // Instantiate our dependencies
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        services.AddSingleton(typeof(CacheHelper));
        
        var config = new SlowDownOptions();
        var provider = services.BuildServiceProvider();
        
        var configuration = provider.GetService<IConfiguration>();
        configuration?.Bind(Constants.ConfigurationKey, config);
        
        services.AddSingleton(config);
        
        if (config.SlowDownEnabled)
            configAction?.Invoke(config);

        return services;
    }

    /// <summary>
    /// Add the <see cref="SlowDownMiddleware"/> in Startup.Configure().
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns></returns>
    public static IApplicationBuilder UseSlowDown(this IApplicationBuilder builder)
    {
        var config = builder.ApplicationServices.GetService(typeof(SlowDownOptions)) as SlowDownOptions;

        if (config?.SlowDownEnabled ?? false)
            builder.UseMiddleware<SlowDownMiddleware>();

        return builder;
    }
}