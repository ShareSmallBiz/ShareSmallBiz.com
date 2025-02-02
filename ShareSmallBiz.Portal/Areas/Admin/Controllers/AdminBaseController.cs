using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminBaseController(
        ShareSmallBizUserContext _context,
        UserManager<ShareSmallBizUser> _userManager,
        RoleManager<IdentityRole> _roleManager) : Controller
    {
    }
}
