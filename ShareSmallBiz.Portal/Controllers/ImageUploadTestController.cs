using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ShareSmallBiz.Portal.Controllers
{
    /// <summary>
    /// Simple controller for testing image uploads with extensive logging
    /// </summary>
    public class ImageUploadTestController : Controller
    {
        private readonly ILogger<ImageUploadTestController> _logger;

        public ImageUploadTestController(ILogger<ImageUploadTestController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Image Upload Test page accessed at {Time}", DateTime.UtcNow);
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            try
            {
                _logger.LogInformation("UploadImage action started at {Time}", DateTime.UtcNow);

                if (imageFile == null)
                {
                    _logger.LogWarning("No file was submitted");
                    ViewBag.Error = "No file was submitted";
                    return View("Result");
                }

                _logger.LogInformation("File received: Name={FileName}, Size={FileSize}, ContentType={ContentType}",
                    imageFile.FileName,
                    imageFile.Length,
                    imageFile.ContentType);

                // Check if it's an image
                if (!imageFile.ContentType.StartsWith("image/"))
                {
                    _logger.LogWarning("File is not an image. ContentType={ContentType}", imageFile.ContentType);
                    ViewBag.Error = "The uploaded file is not an image";
                    return View("Result");
                }

                // Read the file content
                using var memoryStream = new MemoryStream();
                _logger.LogDebug("Copying file to memory stream");
                await imageFile.CopyToAsync(memoryStream);

                _logger.LogInformation("File successfully copied to memory stream. Size={StreamSize} bytes",
                    memoryStream.Length);

                // Process image with ImageSharp
                _logger.LogInformation("Starting ImageSharp processing");
                byte[] processedImageBytes = memoryStream.ToArray();

                // Convert to base64 for display
                var base64Data = Convert.ToBase64String(processedImageBytes);
                _logger.LogDebug("Processed image converted to base64. Length={Base64Length}", base64Data.Length);

                // Store image data for result view
                ViewBag.FileName = imageFile.FileName;
                ViewBag.FileSize = imageFile.Length;
                ViewBag.ProcessedSize = processedImageBytes.Length;
                ViewBag.ContentType = imageFile.ContentType;
                ViewBag.ImagePreview = $"data:image/jpeg;base64,{base64Data}";
                ViewBag.Success = true;

                _logger.LogInformation("UploadImage action completed successfully");
                return View("Result");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing image upload");
                ViewBag.Error = ex.ToString();
                return View("Result");
            }
        }

        public IActionResult Result()
        {
            return View();
        }

    }
}