using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminBaseController : Controller
{
    protected readonly ShareSmallBizUserContext _context;
    protected readonly ShareSmallBizUserManager _userManager;
    protected readonly RoleManager<IdentityRole> _roleManager;

    public AdminBaseController(
        ShareSmallBizUserContext context,
        ShareSmallBizUserManager userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }
}
