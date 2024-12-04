using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        var config = new SlowDownOptions();
        var provider = services.BuildServiceProvider();
        
        var configuration = provider.GetService<IConfiguration>();
        configuration?.Bind(Constants.ConfigurationKey, config);
        
        SlowDownOptions.CurrentOptions = config;
        
        if (config.SlowDownEnabled)
            configAction?.Invoke(config);

        return services;
    }

    /// <summary>
    /// Add the <see cref="SlowDownMiddleware"/> in Startup.Configure().
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseSlowDown(this IApplicationBuilder builder)
    {
        var config = SlowDownOptions.CurrentOptions;

        if (config.SlowDownEnabled)
            builder.UseMiddleware<SlowDownMiddleware>();

        return builder;
    }
}