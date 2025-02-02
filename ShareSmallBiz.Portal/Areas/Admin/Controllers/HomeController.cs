using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

public class HomeController(
    ShareSmallBizUserContext _context,
    UserManager<ShareSmallBizUser> _userManager,
    RoleManager<IdentityRole> _roleManager) : AdminBaseController(_context, _userManager, _roleManager)
{
    public IActionResult Index()
    {
        return View();
    }
}
