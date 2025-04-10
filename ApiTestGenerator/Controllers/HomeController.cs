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
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IOpenApiParser openApiParser,
        IApiTestService apiTestService,
        ITestDataService testDataService,
        ILogger<HomeController> logger)
    {
        _openApiParser = openApiParser;
        _apiTestService = apiTestService;
        _testDataService = testDataService;
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
    public IActionResult UploadOpenApi(IFormFile openApiFile)
    {
        if (openApiFile == null || openApiFile.Length == 0)
        {
            return BadRequest("Please upload a valid OpenAPI JSON file");
        }

        try
        {
            using (var streamReader = new StreamReader(openApiFile.OpenReadStream()))
            {
                var json = streamReader.ReadToEnd();
                var apiDefinition = _openApiParser.ParseOpenApiJson(json);

                TempData["ApiDefinition"] = JsonSerializer.Serialize(apiDefinition);

                return RedirectToAction("TestForm");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading OpenAPI file");
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public IActionResult TestForm()
    {
        var apiDefinitionJson = TempData["ApiDefinition"] as string;

        if (string.IsNullOrEmpty(apiDefinitionJson))
        {
            return RedirectToAction("Index");
        }

        var apiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);

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

            // Get the API definition from TempData
            var apiDefinitionJson = TempData["ApiDefinition"] as string;
            TempData.Keep("ApiDefinition"); // Keep for the next request

            if (!string.IsNullOrEmpty(apiDefinitionJson))
            {
                viewModel.ApiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);
            }

            return View("TestForm", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing API test");
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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

            var apiDefinitionJson = TempData["ApiDefinition"] as string;
            TempData.Keep("ApiDefinition");

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

            if (!string.IsNullOrEmpty(apiDefinitionJson))
            {
                viewModel.ApiDefinition = JsonSerializer.Deserialize<ApiDefinition>(apiDefinitionJson);
            }

            return View("TestForm", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading test request");
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}