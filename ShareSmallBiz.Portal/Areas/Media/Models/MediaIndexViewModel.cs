using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShareSmallBiz.Portal.Areas.Media.Models
{
    public class MediaIndexViewModel
    {
        public IEnumerable<MediaModel> Media { get; set; } = [];
        public string? SearchString { get; set; }
        public int? MediaTypeFilter { get; set; }
        public int? StorageProviderFilter { get; set; }
        public List<SelectListItem> MediaTypes { get; set; } = [];
        public List<SelectListItem> StorageProviders { get; set; } = [];
    }
}
