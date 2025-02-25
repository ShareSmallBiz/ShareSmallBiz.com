using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Events;
using Microsoft.Extensions.Hosting;

namespace ShareSmallBiz.Portal.Infrastructure.Logging;

public static class LoggingUtility
{
    public static void ConfigureLogging(WebApplicationBuilder builder, string applicationName)
    {
        // Get log path from configuration or use default.
        // This value can be used within your settings.json configuration if needed.
        string logPath = builder.Configuration.GetValue<string>("ShareSmallBiz:LogFilePath")
                         ?? $"C:\\websites\\ShareSmallBiz\\logs\\{applicationName}-log-.txt";

        // Clear any existing logging providers.
        builder.Logging.ClearProviders();

        // Enable Serilog self-log for troubleshooting in Development environment only.
        if (builder.Environment.IsDevelopment())
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine($"Serilog: {msg}"));
        }

        // Configure Serilog to read settings from configuration.
        // This reads from the "Serilog" section in appsettings.json.
        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(builder.Configuration);

        // Optionally, if you want to ensure a file sink is configured when not present in configuration,
        // you can add a fallback file sink. Comment or remove the following block if you fully configure Serilog via app settings.
        if (!builder.Configuration.GetSection("Serilog:WriteTo").Exists())
        {
            loggerConfiguration = loggerConfiguration.WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error);
        }

        // Create the logger.
        Log.Logger = loggerConfiguration.CreateLogger();

        // Add Serilog to the logging providers.
        builder.Logging.AddProvider(new SerilogLoggerProvider(Log.Logger));

        // Add filtering for specific namespaces as needed.
        builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Identity", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Session", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Http", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Error);

        // Log a test entry to confirm that logging is working.
        Log.Information("Logger setup complete. This is a test log entry.");
    }
}
