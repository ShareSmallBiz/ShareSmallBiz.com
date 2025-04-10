// First, let's create the Media entity class
using System;

namespace ShareSmallBiz.Portal.Data.Enums;


/// <summary>
/// Defines the available sorting options for discussions
/// </summary>
public enum SortType
{
    /// <summary>
    /// Sort by ID (default)
    /// </summary>
    Default = 0,

    /// <summary>
    /// Sort by published date (newest first)
    /// </summary>
    Recent = 1,

    /// <summary>
    /// Sort by popularity (most views first)
    /// </summary>
    Popular = 2
}