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
        // Get log path from configuration or use default
        string logPath = builder.Configuration.GetValue<string>("ShareSmallBiz:LogFilePath")
                         ?? $"C:\\websites\\ShareSmallBiz\\logs\\{applicationName}-log-.txt";

        // Clear existing logging providers
        builder.Logging.ClearProviders();

        // Enable Serilog self-log for troubleshooting in Development environment only
        if (builder.Environment.IsDevelopment())
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine($"Serilog: {msg}"));
        }

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug() // ✅ Enable Debug-Level Logging
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Error)
            .CreateLogger();

        // Add Serilog to the logging providers
        builder.Logging.AddProvider(new SerilogLoggerProvider(Log.Logger));

        // ✅ Enable Debug-Level Logging for Authentication and Identity
        builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Identity", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Session", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Http", LogLevel.Error);

        // Add filtering for EF Core logging
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Error);

        // Log a test entry to confirm setup
        Log.Information("Logger setup complete. This is a test log entry.");
    }

}
