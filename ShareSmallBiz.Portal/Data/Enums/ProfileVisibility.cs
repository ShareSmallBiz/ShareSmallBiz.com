namespace ShareSmallBiz.Portal.Data.Enums;

/// <summary>
/// Defines the visibility settings for user profiles
/// </summary>
public enum ProfileVisibility
{
    /// <summary>
    /// Profile is visible to everyone, including non-authenticated users
    /// </summary>
    Public = 0,
    
    /// <summary>
    /// Profile is only visible to authenticated users
    /// </summary>
    Authenticated = 1,
    
    /// <summary>
    /// Profile is only visible to users who are following or being followed by the user
    /// </summary>
    Connections = 2,
    
    /// <summary>
    /// Profile is only visible to the user themselves
    /// </summary>
    Private = 3
}