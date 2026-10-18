using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ShareSmallBiz.Infrastructure.Email;

/// <summary>Outbound email settings (section <c>Email</c>).</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Log" (Development only). A real provider is added before the first deployment.</summary>
    public string? Provider { get; set; }
}

internal sealed class EmailOptionsValidator(IHostEnvironment environment) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options) => options.Provider switch
    {
        "Log" when environment.IsDevelopment() || environment.IsEnvironment("Testing") => ValidateOptionsResult.Success,
        "Log" => ValidateOptionsResult.Fail("Email:Provider \"Log\" writes confirmation and reset links to logs and is only allowed in Development."),
        null or "" when environment.IsEnvironment("Testing") => ValidateOptionsResult.Success,
        null or "" => ValidateOptionsResult.Fail("Email:Provider is not configured; account emails cannot be sent."),
        var other => ValidateOptionsResult.Fail($"Email:Provider \"{other}\" is not supported."),
    };
}
