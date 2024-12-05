using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Xunit;
using Xunit.Abstractions;

namespace SlowDown.Tests;

public class AspNetTestServerFixture(ITestOutputHelper output) : WebApplicationFactory<Startup>
{
    private IHost _host = null!;
    private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _host = builder.Build();
        _host.Start();
        return _host;
    }

    protected override IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureLogging((_, builder) =>
            {
                builder.Services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(_output));
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseTestServer();
            });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseStartup<Startup>();
    }
}