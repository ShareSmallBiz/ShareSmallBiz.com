using System.ComponentModel.DataAnnotations;

namespace ShareSmallBiz.Api.Common;

/// <summary>Accepts only absolute http/https URLs (rejects javascript:, data:, relative paths). Null is valid.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class HttpUrlAttribute() : ValidationAttribute("The {0} field must be an absolute http or https URL.")
{
    public override bool IsValid(object? value) =>
        value is null
        || (value is string text
            && Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}
