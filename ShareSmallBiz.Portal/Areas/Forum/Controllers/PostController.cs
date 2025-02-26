using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace ShareSmallBiz.Portal.Areas.Forum.Controllers
{
    [Authorize]
    [Area("Forum")]
    public class PostController(
        DiscussionProvider postService,
        ShareSmallBizUserManager userManager,
        KeywordProvider keywordService,
        ILogger<PostController> logger) : ForumBaseController
    {
        // GET: /Forum/Post/Index
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                return View(await postService.GetAllPostsAsync());
            }
            return View(await postService.GetAllUserPostsAsync());
        }

        [HttpGet, ActionName("MyPosts")]
        public async Task<IActionResult> MyPosts()
        {
            var post = await postService.GetAllUserPostsAsync();
            if (post == null)
            {
                return NotFound();
            }
            return View("index", post);
        }

        // GET: /Forum/Post/Create
        public async Task<IActionResult> Create()
        {
            var discussionModel = new DiscussionModel();
            discussionModel.Keywords = await GetCachedKeywordNamesAsync();
            return View("Edit", discussionModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiscussionModel discussionModel)
        {
            ClaimsPrincipal currentUser = this.User;
            if (!ModelState.IsValid)
            {
                // In case of error, also pass cached keywords back to the view
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                return View("Edit", discussionModel);
            }
            try
            {
                var user = await userManager.GetUserAsync(User);
                if (user == null)
                {
                    logger.LogWarning("User not found when creating discussionModel.");
                    return Unauthorized();
                }
                discussionModel.CreatedID = user.Id;
                await postService.CreatePostAsync(discussionModel, currentUser);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating discussionModel");
                ModelState.AddModelError("", "An error occurred while creating the discussionModel.");
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                return View("Edit", discussionModel);
            }
        }

        // GET: /Forum/Post/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var discussionModel = await postService.GetPostByIdAsync(id);
            if (discussionModel == null)
            {
                return NotFound();
            }
            var user = await userManager.GetUserAsync(User);

            if (!User.IsInRole("Admin"))
            {
                if (discussionModel.Author.Id != user.Id)
                {
                    return RedirectToAction("Index");
                }
            }


            // Pass cached keyword names to the view
            discussionModel.Keywords = await GetCachedKeywordNamesAsync();
            return View(discussionModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DiscussionModel discussionModel)
        {
            ClaimsPrincipal currentUser = this.User;
            if (!ModelState.IsValid)
            {
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                return View(discussionModel);
            }
            try
            {
                var user = await userManager.GetUserAsync(User);
                if (user == null)
                {
                    logger.LogWarning("User not found when updating discussionModel.");
                    return Unauthorized();
                }

                discussionModel.ModifiedID = user.Id;
                var success = await postService.UpdatePostAsync(discussionModel, currentUser);
                if (!success)
                {
                    logger.LogError("Error updating discussionModel");
                    discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                    return View(discussionModel);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating discussionModel");
                ModelState.AddModelError("", "An error occurred while updating the discussionModel.");
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                return View(discussionModel);
            }
        }

        // GET: /Forum/Post/Delete/{id}
        public async Task<IActionResult> Delete(int id)
        {
            var post = await postService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await postService.DeletePostAsync(id);
                if (!success)
                {
                    return NotFound();
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting discussionModel");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Retrieves a cached list of keyword names from the session.
        /// If not present, fetches from the KeywordProvider, caches it, and returns the list.
        /// </summary>
        private async Task<List<string>> GetCachedKeywordNamesAsync()
        {
            const string cacheKey = "CachedKeywordNames";
            var cachedJson = HttpContext.Session.GetString(cacheKey);
            List<string> keywordNames;
            if (!string.IsNullOrEmpty(cachedJson))
            {
                keywordNames = JsonSerializer.Deserialize<List<string>>(cachedJson);
            }
            else
            {
                // Assume GetAllKeywordsAsync returns a collection of keyword entities with a Name property
                var keywords = await keywordService.GetAllKeywordsAsync();
                keywordNames = keywords.Select(k => k.Name).ToList();
                HttpContext.Session.SetString(cacheKey, JsonSerializer.Serialize(keywordNames));
            }
            return keywordNames;
        }
    }
}
