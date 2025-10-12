using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace MentalHealthBar.Api.Tests;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Add a custom logger that outputs to console for test visibility
            services.AddSingleton<ILoggerFactory>(provider =>
            {
                var logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File($"logs/test-{DateTime.Now:yyyyMMdd-HHmmss}.log",
                        rollingInterval: RollingInterval.Infinite,
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                return new LoggerFactory().AddSerilog(logger);
            });
        });

        builder.UseEnvironment("Test");

        // Use test-specific configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            config.AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: false);
        });

        base.ConfigureWebHost(builder);
    }
}