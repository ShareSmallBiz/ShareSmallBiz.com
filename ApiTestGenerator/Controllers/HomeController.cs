using ApiTestGenerator.Models;
using ApiTestGenerator.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace ApiTestGenerator.Controllers;

public class HomeController : Controller
{
    private readonly IOpenApiParser _openApiParser;
    private readonly IApiTestService _apiTestService;
    private readonly ITestDataService _testDataService;
    private readonly ITemporaryStorageService _tempStorage;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IOpenApiParser openApiParser,
        IApiTestService apiTestService,
        ITestDataService testDataService,
        ITemporaryStorageService tempStorage,
        ILogger<HomeController> logger)
    {
        _openApiParser = openApiParser;
        _apiTestService = apiTestService;
        _testDataService = testDataService;
        _tempStorage = tempStorage;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var viewModel = new ApiTestViewModel
        {
            SavedTests = _testDataService.GetAllTestRequests()
        };

        return View(viewModel);
    }

    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
    [RequestSizeLimit(52428800)] // 50MB
    public async Task<IActionResult> UploadOpenApi(IFormFile openApiFile)
    {
        if (openApiFile == null || openApiFile.Length == 0)
        {
            return BadRequest("Please upload a valid OpenAPI JSON file");
        }

        try
        {
            _logger.LogInformation("Processing OpenAPI file: {FileName}, Size: {FileSize} bytes",
                openApiFile.FileName, openApiFile.Length);

            ApiDefinition apiDefinition;
            using (var stream = openApiFile.OpenReadStream())
            {
                apiDefinition = await _openApiParser.ParseOpenApiStreamAsync(stream);
            }

            // Initialize Servers collection if it's null
            if (apiDefinition.Servers == null)
            {
                apiDefinition.Servers = new Dictionary<string, ServerObject>();
            }

            // Add or update servers programmatically
            apiDefinition.Servers["production"] = new ServerObject
            {
                Url = "https://sharesmallbiz.com/",
                Description = "Production Server"
            };

            // Store in session instead of TempData for large objects
            _tempStorage.StoreObject("ApiDefinition", apiDefinition);

            return RedirectToAction("TestForm");
        }
        catch (Exception ex)
        {
            var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            _logger.LogError(ex, "Error uploading OpenAPI file: {ErrorMessage}", errorMessage);

            // Add file info to the log
            _logger.LogError("File information: Name={FileName}, Size={FileSize}",
                openApiFile.FileName,
                openApiFile.Length);

            ModelState.AddModelError("", $"Failed to process OpenAPI file: {errorMessage}");
            return View("Index", new ApiTestViewModel
            {
                SavedTests = _testDataService.GetAllTestRequests()
            });
        }
    }

    public IActionResult TestForm()
    {
        ApiDefinition? apiDefinition = null;

        // Try getting from session first
        if (_tempStorage.HasObject("ApiDefinition"))
        {
            apiDefinition = _tempStorage.RetrieveObject<ApiDefinition>("ApiDefinition");
        }
        // Fall back to TempData for backward compatibility
        else
        {
            var apiDefinitionJson = TempData["ApiDefinition"] as string;
            if (!string.IsNullOrEmpty(apiDefinitionJson))
            {
                apiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);
            }
        }

        if (apiDefinition == null)
        {
            return RedirectToAction("Index");
        }

        var viewModel = new ApiTestViewModel
        {
            ApiDefinition = apiDefinition,
            SavedTests = _testDataService.GetAllTestRequests()
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ExecuteTest(ApiTestViewModel viewModel)
    {
        try
        {
            var (responseContent, status) = await _apiTestService.ExecuteApiTest(viewModel);

            viewModel.ApiResponse = responseContent;
            viewModel.ApiResponseStatus = status;
            viewModel.SavedTests = _testDataService.GetAllTestRequests();

            // Get the API definition from session first, then fall back to TempData
            ApiDefinition? apiDefinition = null;

            if (_tempStorage.HasObject("ApiDefinition"))
            {
                apiDefinition = _tempStorage.RetrieveObject<ApiDefinition>("ApiDefinition");
            }
            else
            {
                var apiDefinitionJson = TempData["ApiDefinition"] as string;
                TempData.Keep("ApiDefinition");

                if (!string.IsNullOrEmpty(apiDefinitionJson))
                {
                    apiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);
                }
            }

            if (apiDefinition != null)
            {
                viewModel.ApiDefinition = apiDefinition;
            }

            return View("TestForm", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing API test");
            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = ex.Message
            });
        }
    }

    [HttpPost]
    public IActionResult SaveTest(ApiTestViewModel viewModel)
    {
        try
        {
            if (string.IsNullOrEmpty(viewModel.SavedTestName))
            {
                ModelState.AddModelError("SavedTestName", "Test name is required");
                return View("TestForm", viewModel);
            }

            _testDataService.SaveTestRequest(viewModel);

            return RedirectToAction("TestForm");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving test request");
            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = ex.Message
            });
        }
    }

    [HttpGet]
    public IActionResult LoadTest(string id)
    {
        try
        {
            var test = _testDataService.GetTestRequest(id);

            if (test == null)
            {
                return NotFound();
            }

            // Get API definition from session first, then TempData
            ApiDefinition? apiDefinition = null;

            if (_tempStorage.HasObject("ApiDefinition"))
            {
                apiDefinition = _tempStorage.RetrieveObject<ApiDefinition>("ApiDefinition");
            }
            else
            {
                var apiDefinitionJson = TempData["ApiDefinition"] as string;
                TempData.Keep("ApiDefinition");

                if (!string.IsNullOrEmpty(apiDefinitionJson))
                {
                    apiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);
                }
            }

            var viewModel = new ApiTestViewModel
            {
                SelectedServer = test.BaseUrl,
                SelectedPath = test.Path,
                SelectedMethod = test.Method,
                PathParameters = test.PathParameters,
                QueryParameters = test.QueryParameters,
                Headers = test.Headers,
                RequestBody = test.RequestBody,
                SavedTestName = test.Name,
                SavedTests = _testDataService.GetAllTestRequests()
            };

            if (apiDefinition != null)
            {
                viewModel.ApiDefinition = apiDefinition;
            }

            return View("TestForm", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading test request");
            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = ex.Message
            });
        }
    }

    [HttpPost]
    public IActionResult DeleteTest(string id)
    {
        try
        {
            _testDataService.DeleteTestRequest(id);
            return RedirectToAction("TestForm");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting test request");
            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = ex.Message
            });
        }
    }

    public IActionResult ManageServers()
    {
        ApiDefinition? apiDefinition = null;

        // Try getting from session first
        if (_tempStorage.HasObject("ApiDefinition"))
        {
            apiDefinition = _tempStorage.RetrieveObject<ApiDefinition>("ApiDefinition");
        }
        else
        {
            var apiDefinitionJson = TempData["ApiDefinition"] as string;
            if (!string.IsNullOrEmpty(apiDefinitionJson))
            {
                apiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);
            }
        }

        if (apiDefinition == null)
        {
            return RedirectToAction("Index");
        }

        return View(apiDefinition);
    }

    [HttpPost]
    public IActionResult AddServer(string serverKey, string url, string description)
    {
        ApiDefinition? apiDefinition = null;

        if (_tempStorage.HasObject("ApiDefinition"))
        {
            apiDefinition = _tempStorage.RetrieveObject<ApiDefinition>("ApiDefinition");
        }
        else
        {
            var apiDefinitionJson = TempData["ApiDefinition"] as string;
            TempData.Keep("ApiDefinition");

            if (!string.IsNullOrEmpty(apiDefinitionJson))
            {
                apiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);
            }
        }

        if (apiDefinition == null)
        {
            return RedirectToAction("Index");
        }

        if (apiDefinition.Servers == null)
        {
            apiDefinition.Servers = new Dictionary<string, ServerObject>();
        }

        apiDefinition.Servers[serverKey] = new ServerObject
        {
            Url = url,
            Description = description
        };

        _tempStorage.StoreObject("ApiDefinition", apiDefinition);

        return RedirectToAction("ManageServers");
    }

    [HttpPost]
    public IActionResult RemoveServer(string serverKey)
    {
        ApiDefinition? apiDefinition = null;

        if (_tempStorage.HasObject("ApiDefinition"))
        {
            apiDefinition = _tempStorage.RetrieveObject<ApiDefinition>("ApiDefinition");
        }
        else
        {
            var apiDefinitionJson = TempData["ApiDefinition"] as string;
            TempData.Keep("ApiDefinition");

            if (!string.IsNullOrEmpty(apiDefinitionJson))
            {
                apiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);
            }
        }

        if (apiDefinition == null || apiDefinition.Servers == null)
        {
            return RedirectToAction("Index");
        }

        if (apiDefinition.Servers.ContainsKey(serverKey))
        {
            apiDefinition.Servers.Remove(serverKey);
        }

        _tempStorage.StoreObject("ApiDefinition", apiDefinition);

        return RedirectToAction("ManageServers");
    }


}