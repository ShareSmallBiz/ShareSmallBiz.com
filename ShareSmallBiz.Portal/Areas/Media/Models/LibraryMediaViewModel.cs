using Microsoft.AspNetCore.Mvc.Rendering;
using ShareSmallBiz.Portal.Data.Enums;

namespace ShareSmallBiz.Portal.Areas.Media.Models
{
    public class LibraryMediaViewModel
    {
        public LibraryMediaViewModel()
        {
        }

        public LibraryMediaViewModel(MediaModel media)
        {
            Id = media.Id;
            FileName = media.FileName;
            MediaType = (int)media.MediaType;
            StorageProvider = (int)media.StorageProvider;
            Description = media.Description;
            Attribution = media.Attribution;
            IsExternalLink = media.StorageProvider == StorageProviderNames.External || media.StorageProvider == StorageProviderNames.YouTube;
            IsYouTube = media.StorageProvider == StorageProviderNames.YouTube;
            ExternalUrl = media.StorageProvider == StorageProviderNames.External ? media.Url : string.Empty;
            YouTubeUrl = media.StorageProvider == StorageProviderNames.YouTube ? media.Url : string.Empty;
            ContentType = media.ContentType;
            FileSize = media.FileSize;
            Url = media.Url;
        }

        public int Id { get; set; }

        [Display(Name = "Is YouTube Video")]
        public bool IsYouTube { get; set; }

        [Display(Name = "YouTube URL")]
        [Url(ErrorMessage = "Please enter a valid YouTube URL")]
        public string? YouTubeUrl { get; set; }

        [Required(ErrorMessage = "File name is required")]
        [StringLength(255, ErrorMessage = "File name cannot exceed 255 characters")]
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Media type is required")]
        [Display(Name = "Media Type")]
        public int MediaType { get; set; }

        public List<SelectListItem>? MediaTypes { get; set; } = [];

        [Display(Name = "Storage Provider")]
        public int StorageProvider { get; set; }

        public List<SelectListItem>? StorageProviders { get; set; } = [];

        [StringLength(512, ErrorMessage = "Description cannot exceed 512 characters")]
        public string Description { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Attribution cannot exceed 255 characters")]
        [Display(Name = "Attribution (if applicable)")]
        public string Attribution { get; set; } = string.Empty;

        [Display(Name = "Is External Link")]
        public bool IsExternalLink { get; set; }

        [Display(Name = "File")]
        public IFormFile? File { get; set; }

        [Display(Name = "External URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ExternalUrl { get; set; }

        public string? ContentType { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? Url { get; set; } = string.Empty;
        public string? YouTubeVideoId { get; set; } = string.Empty;
        public string StorageMetadata { get; set; } = string.Empty;
    }
}