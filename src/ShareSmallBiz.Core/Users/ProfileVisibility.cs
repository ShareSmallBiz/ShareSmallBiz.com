using System.Text.Json.Serialization;

namespace ShareSmallBiz.Core.Users;

/// <summary>Serialized by name ("Public"/"Private") everywhere, including the OpenAPI contract.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProfileVisibility>))]
public enum ProfileVisibility
{
    /// <summary>Listed in the directory and viewable by anyone.</summary>
    Public = 0,

    /// <summary>Hidden from everyone but the member; lookups return 404 so existence is not revealed.</summary>
    Private = 1,
}
