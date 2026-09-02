using ADMISION.ENTITIES.Constants;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.Admin + "," + AppConstants.Roles.SuperAdmin)]
    [Route("admin/info-management/ubigeo")]
    public class UbigeoController : Controller
    {
        private readonly IUbigeoService _ubigeo;
        private readonly ILogger<UbigeoController> _logger;

        public UbigeoController(IUbigeoService ubigeo, ILogger<UbigeoController> logger)
        {
            _ubigeo = ubigeo;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var counts = await _ubigeo.GetCountsAsync(ct);
            ViewBag.DeptCount = counts.Departments;
            ViewBag.ProvCount = counts.Provinces;
            ViewBag.DistCount = counts.Districts;
            ViewBag.Countries = await _ubigeo.GetCountriesAsync(ct);

            return View("~/Pages/Admin/InfoManagement/Ubigeo/Index.cshtml");
        }

        [HttpPost]
        [Route("ImportCsv")]
        public async Task<IActionResult> ImportCsv(IFormFile file, Guid countryId, CancellationToken ct)
        {
            if (countryId == Guid.Empty)
            {
                TempData["Error"] = "Por favor selecciona un país.";
                return RedirectToAction(nameof(Index));
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Por favor selecciona un archivo CSV.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = file.OpenReadStream();
                var actor = User.Identity?.Name ?? "System";
                var result = await _ubigeo.ImportCsvAsync(stream, countryId, actor, ct);
                TempData["Success"] = $"Importación finalizada. Nuevos: {result.NewDepartments} Dept, {result.NewProvinces} Prov, {result.NewDistricts} Dist.";
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
            }

            return RedirectToAction(nameof(Index));
        }

        // ── API JSON: obtener árbol completo de ubigeo ──────────────────────
        [HttpGet]
        [Route("GetUbigeos")]
        public async Task<IActionResult> GetUbigeos([FromQuery] Guid countryId, CancellationToken ct)
        {
            if (countryId == Guid.Empty)
                return Json(new { error = "Seleccione un país" });

            var data = await _ubigeo.GetFullUbigeoDataAsync(countryId, ct);
            return Json(data);
        }

        // ── CRUD Departamentos ──────────────────────────────────────────────
        [HttpPost]
        [Route("Department/Create")]
        public async Task<IActionResult> CreateDepartment([FromBody] UbigeoCreateRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, error = "Nombre y código son requeridos" });

            var actor = User.Identity?.Name ?? "System";
            var dept = await _ubigeo.CreateDepartmentAsync(request.Name, request.Code, request.ParentId!.Value, actor, ct);
            return Json(new { success = true, id = dept.Id, name = dept.Name, code = dept.Code });
        }

        [HttpPut]
        [Route("Department/Update/{id}")]
        public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UbigeoUpdateRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, error = "Nombre y código son requeridos" });

            var actor = User.Identity?.Name ?? "System";
            var dept = await _ubigeo.UpdateDepartmentAsync(id, request.Name, request.Code, actor, ct);
            return Json(new { success = true, id = dept.Id, name = dept.Name, code = dept.Code });
        }

        [HttpDelete]
        [Route("Department/Delete/{id}")]
        public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken ct)
        {
            try
            {
                await _ubigeo.DeleteDepartmentAsync(id, ct);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── CRUD Provincias ─────────────────────────────────────────────────
        [HttpPost]
        [Route("Province/Create")]
        public async Task<IActionResult> CreateProvince([FromBody] UbigeoCreateRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, error = "Nombre y código son requeridos" });

            var actor = User.Identity?.Name ?? "System";
            var prov = await _ubigeo.CreateProvinceAsync(request.Name, request.Code, request.ParentId!.Value, actor, ct);
            return Json(new { success = true, id = prov.Id, name = prov.Name, code = prov.Code });
        }

        [HttpPut]
        [Route("Province/Update/{id}")]
        public async Task<IActionResult> UpdateProvince(Guid id, [FromBody] UbigeoUpdateRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, error = "Nombre y código son requeridos" });

            var actor = User.Identity?.Name ?? "System";
            var prov = await _ubigeo.UpdateProvinceAsync(id, request.Name, request.Code, actor, ct);
            return Json(new { success = true, id = prov.Id, name = prov.Name, code = prov.Code });
        }

        [HttpDelete]
        [Route("Province/Delete/{id}")]
        public async Task<IActionResult> DeleteProvince(Guid id, CancellationToken ct)
        {
            try
            {
                await _ubigeo.DeleteProvinceAsync(id, ct);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── CRUD Distritos ──────────────────────────────────────────────────
        [HttpPost]
        [Route("District/Create")]
        public async Task<IActionResult> CreateDistrict([FromBody] UbigeoCreateRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, error = "Nombre y código son requeridos" });

            var actor = User.Identity?.Name ?? "System";
            var dist = await _ubigeo.CreateDistrictAsync(request.Name, request.Code, request.ParentId!.Value, actor, ct);
            return Json(new { success = true, id = dist.Id, name = dist.Name, code = dist.Code });
        }

        [HttpPut]
        [Route("District/Update/{id}")]
        public async Task<IActionResult> UpdateDistrict(Guid id, [FromBody] UbigeoUpdateRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, error = "Nombre y código son requeridos" });

            var actor = User.Identity?.Name ?? "System";
            var dist = await _ubigeo.UpdateDistrictAsync(id, request.Name, request.Code, actor, ct);
            return Json(new { success = true, id = dist.Id, name = dist.Name, code = dist.Code });
        }

        [HttpDelete]
        [Route("District/Delete/{id}")]
        public async Task<IActionResult> DeleteDistrict(Guid id, CancellationToken ct)
        {
            try
            {
                await _ubigeo.DeleteDistrictAsync(id, ct);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }

    // ── Request DTOs ───────────────────────────────────────────────────────
    public class UbigeoCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
    }

    public class UbigeoUpdateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
