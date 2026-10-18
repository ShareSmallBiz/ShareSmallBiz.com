using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShareSmallBiz.Core.Email;
using ShareSmallBiz.Infrastructure.Data;
using ShareSmallBiz.Infrastructure.Email;

namespace ShareSmallBiz.Infrastructure;

public static class DependencyInjection
{
    /// <param name="validateOnStart">False only for build-time OpenAPI generation, which runs without configuration.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, bool validateOnStart = true)
    {
        // Resolved lazily so build-time OpenAPI generation does not need a connection string
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseSqlite(sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.")));

        var email = services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.SectionName);
        if (validateOnStart)
            email.ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        return services;
    }

    /// <summary>Applies pending EF Core migrations (single-instance SQLite deployment).</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
}
