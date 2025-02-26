using Serilog;
using ShareSmallBiz.Portal.Extensions;
using System;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class StorageProvider : IStorageProvider
{
    private readonly string _storageRoot;
    private readonly IHttpClientFactory _httpClientFactory;

    public StorageProvider(IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
    {
        _storageRoot = Path.Combine(env.WebRootPath, "data");
        _httpClientFactory = httpClientFactory;
    }

    private static string GetFileName(string fileName)
    {
        if (fileName.Contains(Path.DirectorySeparatorChar))
        {
            fileName = Path.GetFileName(fileName);
        }

        if (fileName.StartsWith("mceclip0"))
        {
            fileName = $"{Guid.NewGuid():N}.png";
        }

        return fileName.SanitizePath();
    }

    private static string GetImgSrcValue(string imgTag)
    {
        if (!(imgTag.Contains("data:image") && imgTag.Contains("src=")))
            return imgTag;

        int start = imgTag.IndexOf("src=");
        int srcStart = imgTag.IndexOf("\"", start) + 1;
        if (srcStart < 2) return imgTag;

        int srcEnd = imgTag.IndexOf("\"", srcStart);
        if (srcEnd < 1 || srcEnd <= srcStart) return imgTag;

        return imgTag.Substring(srcStart, srcEnd - srcStart);
    }

    private string PathToUrl(string path)
    {
        return $"data/{path.Replace(_storageRoot, string.Empty).TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/')}";
    }

    private static string TitleFromUri(Uri uri)
    {
        var title = uri.ToString().ToLower();
        title = title.Replace("%2f", "/").Replace(" ", "-");

        if (title.Contains("/"))
        {
            title = Path.GetFileName(title);
        }

        return $"{Guid.NewGuid():N}.png";
    }

    private void VerifyPath(string path)
    {
        var dir = Path.Combine(_storageRoot, path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public bool FileExists(string path)
    {
        try
        {
            return File.Exists(Path.Combine(_storageRoot, path));
        }
        catch (Exception ex)
        {
            Log.Error($"Error checking file existence: {ex.Message}");
            return false;
        }
    }

    public async Task<string> UploadBase64Image(string baseImg, string root, string path = "")
    {
        try
        {
            path = path.Replace("/", Path.DirectorySeparatorChar.ToString());
            VerifyPath(path);

            string imgSrc = GetImgSrcValue(baseImg);
            string fileName = $"{Guid.NewGuid():N}.png";

            if (imgSrc.StartsWith("data:image/png;base64,"))
            {
                imgSrc = imgSrc.Replace("data:image/png;base64,", string.Empty);
            }
            else if (imgSrc.StartsWith("data:image/jpeg;base64,"))
            {
                fileName = $"{Guid.NewGuid():N}.jpeg";
                imgSrc = imgSrc.Replace("data:image/jpeg;base64,", string.Empty);
            }
            else if (imgSrc.StartsWith("data:image/gif;base64,"))
            {
                fileName = $"{Guid.NewGuid():N}.gif";
                imgSrc = imgSrc.Replace("data:image/gif;base64,", string.Empty);
            }

            var filePath = string.IsNullOrEmpty(path) ?
                Path.Combine(_storageRoot, fileName) :
                Path.Combine(_storageRoot, path, fileName);

            await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(imgSrc));

            return $"![{fileName}]({root}{PathToUrl(filePath)})";
        }
        catch (Exception ex)
        {
            Log.Error($"Error uploading base64 image: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UploadFormFile(IFormFile file, string path = "")
    {
        try
        {
            path = path.Replace("/", Path.DirectorySeparatorChar.ToString());
            VerifyPath(path);

            var fileName = GetFileName(file.FileName);
            var filePath = string.IsNullOrEmpty(path) ?
                Path.Combine(_storageRoot, fileName) :
                Path.Combine(_storageRoot, path, fileName);

            Log.Information($"Uploading file: {filePath}");

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            Log.Information($"Uploaded file: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Error uploading file: {ex.Message}");
            return false;
        }
    }

    public async Task<string> UploadFromWeb(Uri requestUri, string root, string path = "")
    {
        try
        {
            path = path.Replace("/", Path.DirectorySeparatorChar.ToString());
            VerifyPath(path);

            var fileName = TitleFromUri(requestUri);
            var filePath = string.IsNullOrEmpty(path) ?
                Path.Combine(_storageRoot, fileName) :
                Path.Combine(_storageRoot, path, fileName);

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            using (var fs = new FileStream(filePath, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }

            return $"![{fileName}]({root}{PathToUrl(filePath)})";
        }
        catch (Exception ex)
        {
            Log.Error($"Error downloading file from web: {ex.Message}");
            throw;
        }
    }
}
