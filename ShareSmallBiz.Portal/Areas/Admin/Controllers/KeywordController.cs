using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Linq;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;


public class KeywordController(KeywordProvider keywordService,
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager _userManager,
    RoleManager<IdentityRole> _roleManager) : AdminBaseController(_context, _userManager, _roleManager)
{

    // GET: /Keyword/
    // Lists all keywords.
    public async Task<IActionResult> Index()
    {
        var keywords = await keywordService.GetAllKeywordsAsync();
        return View(keywords.OrderBy(o => o.Name));
    }

    // GET: /Keyword/Create
    // Returns the Edit view with an empty model for creating a new keyword.
    public IActionResult Create()
    {
        var model = new KeywordModel();
        return View("Edit", model);
    }

    // GET: /Keyword/Edit/{id}
    // Returns the Edit view populated with the keyword data.
    public async Task<IActionResult> Edit(int id)
    {
        var model = await keywordService.GetKeywordByIdAsync(id);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }

    // POST: /Keyword/Save
    // Handles both creation (if Id is 0) and update.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(KeywordModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        if (model.Id == 0)
        {
            // Create new keyword.
            await keywordService.CreateKeywordAsync(model);
        }
        else
        {
            // Update existing keyword.
            await keywordService.UpdateKeywordAsync(model.Id, model);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Keyword/Delete/{id}
    // Optionally display a confirmation for deletion.
    public async Task<IActionResult> Delete(int id)
    {
        var model = await keywordService.GetKeywordByIdAsync(id);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }

    // POST: /Keyword/Delete/{id}
    // Confirms deletion of a keyword.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await keywordService.DeleteKeywordAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // GET: /Keyword/Upload
    // Returns a partial view that contains the file upload form.
    public IActionResult Upload()
    {
        return PartialView("_UploadPartial");
    }

    // POST: /Keyword/Upload
    // Processes the uploaded CSV file.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a non-empty CSV file to upload.");
            return PartialView("_UploadPartial");
        }

        try
        {
            using var streamReader = new StreamReader(file.OpenReadStream());
            while (!streamReader.EndOfStream)
            {
                var line = await streamReader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Assuming CSV format: keyword,description
                var parts = line.Split(',', 2);  // Split into two parts only
                if (parts.Length < 2)
                    continue;

                var keywordText = parts[0].Trim();
                var descriptionText = parts[1].Trim();

                if (!string.IsNullOrEmpty(keywordText))
                {
                    var model = new KeywordModel
                    {
                        Name = keywordText,
                        Description = descriptionText
                    };
                    await keywordService.CreateKeywordAsync(model);
                }
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error processing file: {ex.Message}");
            return PartialView("_UploadPartial");
        }

        // Redirect to Index after processing file
        return RedirectToAction(nameof(Index));
    }


}
