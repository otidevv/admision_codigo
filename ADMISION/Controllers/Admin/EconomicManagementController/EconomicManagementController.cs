using ADMISION.ENTITIES.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.EconomicManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/economic-management")]
    public class EconomicManagementController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Pages/Admin/EconomicManagement/Index.cshtml");
        }
    }
}