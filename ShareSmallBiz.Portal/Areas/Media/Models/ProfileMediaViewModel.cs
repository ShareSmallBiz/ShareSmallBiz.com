namespace ShareSmallBiz.Portal.Areas.Media.Models
{
    public class ProfileMediaViewModel
    {
        public bool HasProfilePicture { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // For external URL profile picture upload
        public string? ExternalImageUrl { get; set; }

        // For Unsplash profile picture
        public string? UnsplashPhotoId { get; set; }
    }
}