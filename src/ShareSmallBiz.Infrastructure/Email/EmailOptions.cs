using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ShareSmallBiz.Infrastructure.Email;

/// <summary>Outbound email settings (section <c>Email</c>).</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public const string LogProvider = "Log";

    public const string PickupProvider = "Pickup";

    /// <summary>"Log" or "Pickup" (both Development only). A real provider is added before the first deployment.</summary>
    public string? Provider { get; set; }

    /// <summary>Folder the "Pickup" provider writes messages to.</summary>
    public string PickupDirectory { get; set; } = "dev-mail";
}

internal sealed class EmailOptionsValidator(IHostEnvironment environment) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var isLocal = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        return options.Provider switch
        {
            EmailOptions.LogProvider or EmailOptions.PickupProvider when isLocal => ValidateOptionsResult.Success,
            EmailOptions.LogProvider or EmailOptions.PickupProvider => ValidateOptionsResult.Fail(
                $"Email:Provider \"{options.Provider}\" exposes confirmation and reset links locally and is only allowed in Development."),
            null or "" when environment.IsEnvironment("Testing") => ValidateOptionsResult.Success,
            null or "" => ValidateOptionsResult.Fail("Email:Provider is not configured; account emails cannot be sent."),
            var other => ValidateOptionsResult.Fail($"Email:Provider \"{other}\" is not supported."),
        };
    }
}
