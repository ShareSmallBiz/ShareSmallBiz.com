namespace ShareSmallBiz.Portal.Areas.Forum.Controllers
{
    [Area("Forum")]
    [Authorize(Roles = "User")]
    public class ForumBaseController : Controller
    {
    }
}
