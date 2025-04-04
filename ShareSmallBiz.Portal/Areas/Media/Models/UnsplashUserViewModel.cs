using ShareSmallBiz.Portal.Areas.Media.Services;

namespace ShareSmallBiz.Portal.Areas.Media.Models
{
    public class UnsplashUserViewModel
    {
        public UnsplashUser UserProfile { get; set; } = new();
        public List<UnsplashPhoto> Photos { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 9;
        public int TotalPages { get; set; }
        public int TotalPhotos { get; set; }
    }
}