using ShareSmallBiz.Portal.Areas.Media.Services;

namespace ShareSmallBiz.Portal.Areas.Media.Models
{
    public class UnsplashSearchViewModel
    {
        [Display(Name = "Search Query")]
        [Required(ErrorMessage = "Please enter a search term")]
        public string Query { get; set; } = string.Empty;

        [Display(Name = "Page")]
        public int Page { get; set; } = 1;

        [Display(Name = "Results Per Page")]
        [Range(1, 30, ErrorMessage = "Please enter a value between 1 and 30")]
        public int PerPage { get; set; } = 9;

        public int TotalPages { get; set; }

        public int TotalResults { get; set; }

        public List<UnsplashPhoto> Photos { get; set; } = new();

        public List<string> PopularCategories { get; set; } = new();
    }
}
