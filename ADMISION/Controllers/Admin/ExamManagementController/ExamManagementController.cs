using ADMISION.ENTITIES.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management")]
    public class ExamManagementController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Pages/Admin/ExamManagement/Index.cshtml");
        }
    }
}
