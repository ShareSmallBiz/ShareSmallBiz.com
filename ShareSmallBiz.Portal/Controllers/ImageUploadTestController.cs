using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

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
                byte[] originalImageBytes = memoryStream.ToArray();
                byte[] processedImageBytes = await OptimizeImageWithImageSharp(originalImageBytes);

                _logger.LogInformation("Image processing complete. Original size: {OriginalSize} bytes, Processed size: {ProcessedSize} bytes",
                    originalImageBytes.Length, processedImageBytes.Length);

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

        /// <summary>
        /// Optimizes an image using ImageSharp - resizes and compresses
        /// </summary>
        private async Task<byte[]> OptimizeImageWithImageSharp(byte[] originalImage)
        {
            _logger.LogInformation("Beginning ImageSharp optimization of {Size} byte image", originalImage.Length);

            try
            {
                using var inputStream = new MemoryStream(originalImage);
                using var outputStream = new MemoryStream();

                _logger.LogDebug("Loading image with ImageSharp");
                using var image = await Image.LoadAsync(inputStream);

                _logger.LogInformation("Original image dimensions: {Width}x{Height}", image.Width, image.Height);

                // Calculate new dimensions while preserving aspect ratio
                const int maxSize = 250; // Maximum dimension (width or height)
                int width, height;

                if (image.Width > image.Height)
                {
                    width = maxSize;
                    height = (int)(image.Height * ((float)maxSize / image.Width));
                }
                else
                {
                    height = maxSize;
                    width = (int)(image.Width * ((float)maxSize / image.Height));
                }

                _logger.LogInformation("Calculated new dimensions: {Width}x{Height}", width, height);

                // Resize the image
                _logger.LogDebug("Resizing image");
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                }));

                _logger.LogDebug("Saving image as JPEG with 80% quality");
                // Save as JPEG with quality setting
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder
                {
                    Quality = 80
                });

                var result = outputStream.ToArray();
                _logger.LogInformation("Image optimization complete. Result size: {Size} bytes", result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImageSharp processing");
                // Return original if processing fails
                return originalImage;
            }
        }
    }
}