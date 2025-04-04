// First, let's create the Media entity class
using System;

namespace ShareSmallBiz.Portal.Data.Enums
{
    public enum StorageProviderNames
    {
        LocalStorage = 0,      // Stored on your server
        External = 1,   // Linked from external source (like Unsplash)
        YouTube = 4     // Stored on YouTube
    }
}

