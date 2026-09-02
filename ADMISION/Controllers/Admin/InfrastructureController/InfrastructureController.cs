using ADMISION.ENTITIES.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfrastructureController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/infrastructure")]
    public class InfrastructureController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Pages/Admin/Infrastructure/Index.cshtml");
        }
    }
}
