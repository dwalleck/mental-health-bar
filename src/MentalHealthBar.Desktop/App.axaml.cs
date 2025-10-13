using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;
using MentalHealthBar.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace MentalHealthBar.Desktop;

public partial class App : Application
{
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Build the host with DI container
        // CreateDefaultBuilder automatically loads appsettings.json and appsettings.{Environment}.json
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, context);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            })
            .Build();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Create the main window with DI
            var mainWindowViewModel = _host?.Services.GetRequiredService<MainWindowViewModel>()
                ?? new MainWindowViewModel();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel,
            };

            // Start the host
            _host?.StartAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        // Get API configuration (environment-aware)
        var apiBaseUrl = context.Configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured in appsettings.json");

        var timeoutSeconds = int.TryParse(context.Configuration["ApiSettings:Timeout"], out var timeout) ? timeout : 30;
        var maxRetryAttempts = int.TryParse(context.Configuration["ApiSettings:RetryPolicy:MaxRetryAttempts"], out var maxRetries) ? maxRetries : 3;
        var backoffMultiplier = int.TryParse(context.Configuration["ApiSettings:RetryPolicy:BackoffMultiplier"], out var multiplier) ? multiplier : 2;

        // Configure HTTP Client with Polly retry policy
        // All HTTP client configuration is centralized here - ApiClient receives pre-configured client
        services.AddHttpClient<ApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(maxRetryAttempts, backoffMultiplier));

        // Register Services
        services.AddSingleton<ChartingService>();

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<AssessmentsViewModel>();
        services.AddTransient<MoodEntryViewModel>();
        services.AddTransient<HealthMetricsViewModel>();
        services.AddTransient<DataVisualizationViewModel>();
        services.AddTransient<ExportViewModel>();

        // Register Views (if needed for view locator pattern)
        services.AddTransient<MainWindow>();
        services.AddTransient<DashboardView>();
        services.AddTransient<AssessmentsView>();
        services.AddTransient<MoodEntryView>();
        services.AddTransient<HealthMetricsView>();
        services.AddTransient<DataVisualizationView>();
        services.AddTransient<ExportView>();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetryAttempts, int backoffMultiplier)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                maxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(backoffMultiplier, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempts if logger is available
                    Console.WriteLine($"Retry {retryCount}/{maxRetryAttempts} after {timespan.TotalMilliseconds}ms");
                });
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    // Clean up resources when the application exits
    public void Shutdown()
    {
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
    }
}