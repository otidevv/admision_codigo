using ADMISION.ENTITIES.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management")]
    public class InfoController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Pages/Admin/InfoManagement/Index.cshtml");
        }
    }
}