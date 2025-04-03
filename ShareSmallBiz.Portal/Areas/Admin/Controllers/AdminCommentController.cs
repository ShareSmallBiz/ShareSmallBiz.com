using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;
public class AdminCommentController(
ShareSmallBizUserContext _context,
ShareSmallBizUserManager _userManager,
RoleManager<IdentityRole> _roleManager,
AdminCommentService _adminCommentService) : AdminBaseController(_context, _userManager, _roleManager)
{

    // GET: Admin/AdminComment
    public async Task<IActionResult> Index()
    {
        var comments = await _adminCommentService.GetAllCommentsAsync();
        return View(comments);
    }

    // GET: Admin/AdminComment/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var comment = await _adminCommentService.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound();
        }
        return View(comment);
    }

    // GET: Admin/AdminComment/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/AdminComment/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int postId, string content)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        var newComment = await _adminCommentService.CreateCommentAsync(postId, content);
        if (newComment == null)
        {
            ModelState.AddModelError(string.Empty, "Unable to create comment. Please check your inputs and try again.");
            return View();
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/AdminComment/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var comment = await _adminCommentService.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound();
        }
        return View(comment);
    }

    // POST: Admin/AdminComment/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string content)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }
        bool success = await _adminCommentService.UpdateCommentAsync(id, content);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Unable to update comment. Please try again.");
            return View();
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/AdminComment/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await _adminCommentService.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound();
        }
        return View(comment);
    }

    // POST: Admin/AdminComment/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        bool success = await _adminCommentService.DeleteCommentAsync(id);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Unable to delete comment. Please try again.");
            return View();
        }
        return RedirectToAction(nameof(Index));
    }

}
