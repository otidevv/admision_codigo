using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Schools;
using ADMISION.Extensions;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADMISION.Controllers.Admin.SchoolManagement
{
    [Route("admin/colegios")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    public class SchoolManagementController : Controller
    {
        private readonly ISchoolService _schools;
        private readonly IUbigeoService _ubigeo;
        private readonly ILogger<SchoolManagementController> _logger;

        public SchoolManagementController(ISchoolService schools, IUbigeoService ubigeo, ILogger<SchoolManagementController> logger)
        {
            _schools = schools;
            _ubigeo = ubigeo;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            Guid? departmentId, Guid? provinceId, Guid? districtId, string? name,
            int page = 1, int pageSize = 10, string? sortBy = null, string? sortDir = "asc",
            CancellationToken ct = default)
        {
            var query = new SchoolListQuery
            {
                DepartmentId = departmentId,
                ProvinceId = provinceId,
                DistrictId = districtId,
                Name = name,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            var paged = await _schools.ListAsync(query, ct);

            // AJAX: el front consume DataTableResponse<>; mapeamos a la forma esperada.
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new DataTableResponse<object>
                {
                    Data = paged.Items.Select(s => (object)new
                    {
                        s.Id,
                        s.Name,
                        s.Code,
                        s.UgelName,
                        s.Modality,
                        s.Level,
                        s.Management,
                        s.Address,
                        Distrit = s.DistrictName == null ? null : new
                        {
                            Name = s.DistrictName,
                            Province = s.ProvinceName == null ? null : new
                            {
                                Name = s.ProvinceName,
                                Department = s.DepartmentName == null ? null : new { Name = s.DepartmentName }
                            }
                        }
                    }).ToList(),
                    TotalItems = paged.TotalItems,
                    TotalPages = paged.TotalPages,
                    PageSize = paged.PageSize,
                    CurrentPage = paged.Page
                });
            }

            ViewBag.CurrentPage = paged.Page;
            ViewBag.TotalPages = paged.TotalPages;
            ViewBag.TotalItems = paged.TotalItems;
            ViewBag.PageSize = paged.PageSize;

            // Filtro de Región/Departamento del header (se asume el primer país registrado;
            // si en el futuro se segmenta multi-país, exponer countryId en la query).
            ViewBag.Departments = await GetDefaultDepartmentsAsync(ct);

            // La vista vieja recibía List<Schools> como model. Mantenemos esa compatibilidad
            // proyectando los items paginados a la entidad mínima requerida por la vista.
            // (La vista hace AJAX para el listado real.)
            return View("~/Pages/Admin/SchoolManagement/Index.cshtml", new List<Schools>());
        }

        [HttpGet("nuevo")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            ViewBag.Departments = await BuildDepartmentSelectAsync(null, ct);
            return View("~/Pages/Admin/SchoolManagement/Create.cshtml");
        }

        [HttpPost("nuevo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schools school, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await BuildDepartmentSelectAsync(null, ct);
                return View("~/Pages/Admin/SchoolManagement/Create.cshtml", school);
            }

            await _schools.CreateAsync(school, User.Identity?.Name ?? "System", ct);
            TempData["Success"] = "Colegio registrado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("importar")]
        public IActionResult Import()
        {
            return View("~/Pages/Admin/SchoolManagement/Import.cshtml");
        }

        [HttpPost("importar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile excelFile, CancellationToken ct)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                ModelState.AddModelError("", "Por favor seleccione un archivo Excel.");
                return View("~/Pages/Admin/SchoolManagement/Import.cshtml");
            }

            try
            {
                using var stream = excelFile.OpenReadStream();
                var actor = User.Identity?.Name ?? "System";
                var result = await _schools.ImportFromExcelAsync(stream, actor, ct);

                if (result.Errors.Any())
                {
                    var fileBytes = BuildErrorsWorkbook(result.Errors);
                    TempData["Success"] = $"Se importaron {result.ImportedCount} colegios. Se encontraron {result.Errors.Count} errores que se descargarán a continuación.";
                    return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Errores_Importacion_Colegios_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }

                TempData["Success"] = $"Se importaron {result.ImportedCount} colegios exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View("~/Pages/Admin/SchoolManagement/Import.cshtml");
            }
        }

        // ============ Lookups (delegados a IUbigeoService) ============
        [HttpGet("GetProvinces/{departmentId}")]
        public async Task<IActionResult> GetProvinces(Guid departmentId, CancellationToken ct)
        {
            var provinces = await _ubigeo.GetProvincesAsync(departmentId, ct);
            return Json(provinces.Select(p => new { id = p.Id, name = p.Name }));
        }

        [HttpGet("GetDistricts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(Guid provinceId, CancellationToken ct)
        {
            var districts = await _ubigeo.GetDistrictsAsync(provinceId, ct);
            return Json(districts.Select(d => new { id = d.Id, name = d.Name }));
        }

        // ============ Helpers privados ============

        private async Task<IReadOnlyList<UbigeoOption>> GetDefaultDepartmentsAsync(CancellationToken ct)
        {
            // Intenta cargar departamentos del primer país que tenga al menos uno.
            // Si no hay países o ninguno tiene departamentos, retorna todos.
            var countries = await _ubigeo.GetCountriesAsync(ct);
            foreach (var c in countries)
            {
                var depts = await _ubigeo.GetDepartmentsAsync(c.Id, ct);
                if (depts.Count > 0) return depts;
            }

            // Fallback: carga todos los departamentos sin filtrar por país
            return await _ubigeo.GetAllDepartmentsAsync(ct);
        }

        private async Task<SelectList> BuildDepartmentSelectAsync(Guid? selected, CancellationToken ct)
        {
            var departments = await GetDefaultDepartmentsAsync(ct);
            return new SelectList(departments, nameof(UbigeoOption.Id), nameof(UbigeoOption.Name), selected);
        }

        private static byte[] BuildErrorsWorkbook(IReadOnlyList<SchoolImportError> errors)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Errores");

            sheet.Cell(1, 1).Value = "Región";
            sheet.Cell(1, 2).Value = "Provincia";
            sheet.Cell(1, 3).Value = "Distrito";
            sheet.Cell(1, 4).Value = "Nombre UGEL";
            sheet.Cell(1, 5).Value = "Código Modular IE";
            sheet.Cell(1, 6).Value = "Nombre de la Institución Educativa";
            sheet.Cell(1, 7).Value = "Modalidad / Forma";
            sheet.Cell(1, 8).Value = "Nivel / Ciclo";
            sheet.Cell(1, 9).Value = "Error";

            for (int i = 0; i < errors.Count; i++)
            {
                var (row, error) = (errors[i].Row, errors[i].Error);
                sheet.Cell(i + 2, 1).Value = row.Region;
                sheet.Cell(i + 2, 2).Value = row.Province;
                sheet.Cell(i + 2, 3).Value = row.District;
                sheet.Cell(i + 2, 4).Value = row.Ugel;
                sheet.Cell(i + 2, 5).Value = row.Code;
                sheet.Cell(i + 2, 6).Value = row.Name;
                sheet.Cell(i + 2, 7).Value = row.Modality;
                sheet.Cell(i + 2, 8).Value = row.Level;
                sheet.Cell(i + 2, 9).Value = error;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
