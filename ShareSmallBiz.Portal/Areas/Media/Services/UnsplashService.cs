using Microsoft.Extensions.Configuration;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Data.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Service for interacting with the Unsplash API
/// </summary>
public class UnsplashService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UnsplashService> _logger;
    private readonly string _unsplashKey;
    private readonly MediaService _mediaService;
    private readonly FileUploadService _fileUploadService;

    public UnsplashService(
        IHttpClientFactory httpClientFactory,
        ILogger<UnsplashService> logger,
        IConfiguration configuration,
        MediaService mediaService,
        FileUploadService fileUploadService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _unsplashKey = configuration["Unsplash:AccessKey"] ??
            throw new ArgumentNullException("Unsplash:AccessKey is not configured");
        _mediaService = mediaService;
        _fileUploadService = fileUploadService;
    }

    /// <summary>
    /// Searches Unsplash for images matching the query
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="page">Page number</param>
    /// <param name="perPage">Results per page</param>
    /// <returns>Search response with image results</returns>
    public async Task<UnsplashSearchResponse?> SearchImagesAsync(string query, int page = 1, int perPage = 10)
    {
        try
        {
            // Validate parameters
            perPage = Math.Clamp(perPage, 1, 30); // Unsplash limits per_page to 30
            page = Math.Max(1, page);

            // Build the Unsplash API request
            var encodedQuery = Uri.EscapeDataString(query);
            var requestUrl = $"https://api.unsplash.com/search/photos?query={encodedQuery}&page={page}&per_page={perPage}&client_id={_unsplashKey}";

            // Create HttpClient and send request
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Accept-Version", "v1");

            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UnsplashSearchResponse>(content);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Unsplash API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Unsplash photos with query: {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Gets photos by a specific Unsplash user
    /// </summary>
    /// <param name="username">Unsplash username</param>
    /// <param name="page">Page number</param>
    /// <param name="perPage">Results per page</param>
    /// <returns>User photos response</returns>
    public async Task<UnsplashUserPhotosResponse?> GetUserPhotosAsync(string username, int page = 1, int perPage = 10)
    {
        try
        {
            // Validate parameters
            perPage = Math.Clamp(perPage, 1, 30); // Unsplash limits per_page to 30
            page = Math.Max(1, page);

            // Build the Unsplash API request for user photos
            var requestUrl = $"https://api.unsplash.com/users/{username}/photos?page={page}&per_page={perPage}&client_id={_unsplashKey}";

            // Create HttpClient and send request
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Accept-Version", "v1");

            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var photos = JsonSerializer.Deserialize<List<UnsplashPhoto>>(content);

                // Create a response object with pagination information
                var userPhotosResponse = new UnsplashUserPhotosResponse
                {
                    Photos = photos ?? new List<UnsplashPhoto>(),
                    Page = page,
                    PerPage = perPage
                };

                // Try to get total counts from response headers
                if (response.Headers.TryGetValues("x-total", out var totalValues))
                {
                    if (int.TryParse(totalValues.FirstOrDefault(), out int total))
                    {
                        userPhotosResponse.Total = total;
                        userPhotosResponse.TotalPages = (int)Math.Ceiling((double)total / perPage);
                    }
                }

                return userPhotosResponse;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Unsplash API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unsplash photos for user: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Gets user profile information
    /// </summary>
    /// <param name="username">Unsplash username</param>
    /// <returns>User profile information</returns>
    public async Task<UnsplashUser?> GetUserProfileAsync(string username)
    {
        try
        {
            // Build the Unsplash API request for user profile
            var requestUrl = $"https://api.unsplash.com/users/{username}?client_id={_unsplashKey}";

            // Create HttpClient and send request
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Accept-Version", "v1");

            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UnsplashUser>(content);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Unsplash API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unsplash user profile: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Gets a single photo by ID
    /// </summary>
    /// <param name="photoId">Unsplash photo ID</param>
    /// <returns>Photo details</returns>
    public async Task<UnsplashPhoto?> GetPhotoAsync(string photoId)
    {
        try
        {
            var requestUrl = $"https://api.unsplash.com/photos/{photoId}?client_id={_unsplashKey}";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Accept-Version", "v1");

            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UnsplashPhoto>(content);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Unsplash API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unsplash photo with ID: {PhotoId}", photoId);
            throw;
        }
    }

    /// <summary>
    /// Downloads an image from Unsplash and registers the download with the Unsplash API
    /// </summary>
    /// <param name="downloadUrl">Unsplash download URL</param>
    /// <returns>Image as a byte array</returns>
    public async Task<byte[]> DownloadImageAsync(string downloadUrl)
    {
        try
        {
            // Create HttpClient and send request
            var httpClient = _httpClientFactory.CreateClient();

            // First, trigger the download event at Unsplash
            await httpClient.GetAsync(downloadUrl);

            // Then download the actual image
            // The download URL is separate from the API endpoint and doesn't need authentication
            var imageUrl = downloadUrl.Split('?')[0]; // Remove query parameters

            var response = await httpClient.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Unsplash image download error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Failed to download image: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading image from Unsplash");
            throw;
        }
    }

    /// <summary>
    /// Creates a media entity for an Unsplash photo
    /// </summary>
    /// <param name="photo">Unsplash photo</param>
    /// <param name="userId">User ID</param>
    /// <returns>Media entity</returns>
    public async Task<MediaModel> CreateUnsplashMediaAsync(
    UnsplashPhoto photo,
    string userId)
    {
        if (photo == null)
        {
            throw new ArgumentNullException(nameof(photo));
        }

        // Create a proper attribution string following Unsplash guidelines
        var attribution = FormatAttribution(photo);

        // Create a suitable filename
        var fileName = GenerateFileName(photo);

        // Create metadata
        var metadata = new Dictionary<string, string>
        {
            { "photoId", photo.Id },
            { "source", "unsplash" },
            { "username", photo.User.Username },
            { "name", photo.User.Name },
            { "downloadLocation", photo.Links.Download }
        };

        // Create JSON string for metadata
        var metadataJson = JsonSerializer.Serialize(metadata);

        // Create as external link
        return await _fileUploadService.CreateExternalLinkAsync(
            photo.Urls.Full,
            fileName,
            MediaType.Image,
            userId,
            attribution,
            photo.Description ?? photo.AltDescription,
            storageMetaData: metadataJson);
    }

    private string FormatAttribution(UnsplashPhoto photo)
    {
        // Format proper attribution following Unsplash guidelines
        // https://help.unsplash.com/en/articles/2511315-guideline-attribution
        return $"Photo by {photo.User.Name} on Unsplash";
    }

    private string GenerateFileName(UnsplashPhoto photo)
    {
        // Generate a suitable filename
        var baseFileName = !string.IsNullOrEmpty(photo.Description)
            ? photo.Description
            : (!string.IsNullOrEmpty(photo.AltDescription)
                ? photo.AltDescription
                : $"unsplash-{photo.Id}");

        // Sanitize filename
        baseFileName = SanitizeFileName(baseFileName);

        // Ensure filename is not too long
        if (baseFileName.Length > 50)
        {
            baseFileName = baseFileName.Substring(0, 47) + "...";
        }

        // Append extension
        return $"{baseFileName}.jpg";
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove invalid filename characters
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '-');
        }

        // Replace spaces with dashes
        fileName = fileName.Replace(' ', '-');

        // Remove consecutive dashes
        while (fileName.Contains("--"))
        {
            fileName = fileName.Replace("--", "-");
        }

        // Trim dashes from ends
        fileName = fileName.Trim('-');

        return fileName;
    }
}

#region Unsplash API Models

public class UnsplashSearchResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("results")]
    public List<UnsplashPhoto> Results { get; set; } = new();
}

public class UnsplashUserPhotosResponse
{
    public List<UnsplashPhoto> Photos { get; set; } = new();
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
}

public class UnsplashPhoto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("alt_description")]
    public string? AltDescription { get; set; }

    [JsonPropertyName("urls")]
    public UnsplashUrls Urls { get; set; } = new();

    [JsonPropertyName("links")]
    public UnsplashLinks Links { get; set; } = new();

    [JsonPropertyName("user")]
    public UnsplashUser User { get; set; } = new();
}

public class UnsplashUrls
{
    [JsonPropertyName("raw")]
    public string Raw { get; set; } = string.Empty;

    [JsonPropertyName("full")]
    public string Full { get; set; } = string.Empty;

    [JsonPropertyName("regular")]
    public string Regular { get; set; } = string.Empty;

    [JsonPropertyName("small")]
    public string Small { get; set; } = string.Empty;

    [JsonPropertyName("thumb")]
    public string Thumb { get; set; } = string.Empty;
}

public class UnsplashLinks
{
    [JsonPropertyName("self")]
    public string Self { get; set; } = string.Empty;

    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    [JsonPropertyName("download")]
    public string Download { get; set; } = string.Empty;

    [JsonPropertyName("download_location")]
    public string DownloadLocation { get; set; } = string.Empty;
}

public class UnsplashUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("portfolio_url")]
    public string? PortfolioUrl { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; set; }

    [JsonPropertyName("total_collections")]
    public int TotalCollections { get; set; }

    [JsonPropertyName("links")]
    public UnsplashUserLinks Links { get; set; } = new();

    [JsonPropertyName("profile_image")]
    public UnsplashUserProfileImage ProfileImage { get; set; } = new();
}

public class UnsplashUserProfileImage
{
    [JsonPropertyName("small")]
    public string Small { get; set; } = string.Empty;

    [JsonPropertyName("medium")]
    public string Medium { get; set; } = string.Empty;

    [JsonPropertyName("large")]
    public string Large { get; set; } = string.Empty;
}

public class UnsplashUserLinks
{
    [JsonPropertyName("self")]
    public string Self { get; set; } = string.Empty;

    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    [JsonPropertyName("photos")]
    public string Photos { get; set; } = string.Empty;

    [JsonPropertyName("likes")]
    public string Likes { get; set; } = string.Empty;

    [JsonPropertyName("portfolio")]
    public string Portfolio { get; set; } = string.Empty;
}

#endregion