namespace ShareSmallBiz.Portal.Areas.Media.Models
{
    public class ProfileMediaViewModel
    {
        public bool HasProfilePicture { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Indicates if the user has a legacy profile picture stored as byte array
        public bool HasLegacyProfilePicture { get; set; }

        // For external URL profile picture upload
        public string? ExternalImageUrl { get; set; }

        // For Unsplash profile picture
        public string? UnsplashPhotoId { get; set; }

        // Flag to show migration notice if relevant
        public bool ShowMigrationOption => HasLegacyProfilePicture;
    }
}