namespace ShareSmallBiz.Portal.Infrastructure.Services;

public interface IStorageProvider
{
    bool FileExists(string path);
    Task<string> UploadBase64Image(string baseImg, string root, string path = "");
    Task<bool> UploadFormFile(IFormFile file, string path = "");
    Task<string> UploadFromWeb(Uri requestUri, string root, string path = "");
}
