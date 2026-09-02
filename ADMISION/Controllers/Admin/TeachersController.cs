using ADMISION.ENTITIES.Constants;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADMISION.Controllers.Admin
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    [Route("admin/docentes")]
    public class TeachersController : Controller
    {
        private readonly ITeacherService _teachers;

        public TeachersController(ITeacherService teachers)
        {
            _teachers = teachers;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var teachers = await _teachers.GetAllAsync(ct);
            return View("~/Pages/Admin/Teachers/Index.cshtml", teachers);
        }

        [HttpGet("get-teacher/{id}")]
        public async Task<IActionResult> GetTeacher(Guid id, CancellationToken ct)
        {
            var vm = await _teachers.GetForEditAsync(id, ct);
            if (vm == null) return NotFound();
            return Json(vm);
        }

        [HttpPost("save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] Models.ViewModels.Admin.TeacherFormViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { errors });
            }

            var result = await _teachers.SaveAsync(model, User.Identity?.Name ?? "Admin", ct);
            if (result.NotFound) return NotFound();
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Message).ToList() });
            }
            return Ok(new { success = true });
        }

        [HttpPost("toggle-active/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
        {
            var teacher = await _teachers.ToggleActiveAsync(id, User.Identity?.Name ?? "Admin", ct);
            if (teacher == null) return NotFound();
            return Ok(new { success = true, isActive = teacher.IsActive });
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _teachers.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Docente eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el docente porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el docente.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("check-document")]
        public async Task<IActionResult> CheckDocument(string document, Guid? excludeId, CancellationToken ct)
        {
            var exists = await _teachers.ExistsDocumentAsync(document, excludeId, ct);
            return Json(new { taken = exists });
        }

        [HttpGet("importar")]
        public IActionResult Import()
        {
            return View("~/Pages/Admin/Teachers/Import.cshtml");
        }

        [HttpPost("importar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile excelFile, CancellationToken ct)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                ModelState.AddModelError("", "Por favor seleccione un archivo Excel.");
                return View("~/Pages/Admin/Teachers/Import.cshtml");
            }

            try
            {
                using var stream = excelFile.OpenReadStream();
                var actor = User.Identity?.Name ?? "System";
                var result = await _teachers.ImportFromExcelAsync(stream, actor, ct);

                if (result.Errors.Any())
                {
                    var fileBytes = BuildErrorsWorkbook(result.Errors);
                    TempData["Success"] = $"Se importaron {result.ImportedCount} docentes. Se encontraron {result.Errors.Count} errores que se descargarán a continuación.";
                    return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Errores_Importacion_Docentes_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }

                TempData["Success"] = $"Se importaron {result.ImportedCount} docentes exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Ocurrió un error al procesar el archivo. Verifique que el formato sea correcto.");
                return View("~/Pages/Admin/Teachers/Import.cshtml");
            }
        }

        private static byte[] BuildErrorsWorkbook(IReadOnlyList<TeacherImportError> errors)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Errores");

            sheet.Cell(1, 1).Value = "DNI";
            sheet.Cell(1, 2).Value = "Apellido Paterno";
            sheet.Cell(1, 3).Value = "Apellido Materno";
            sheet.Cell(1, 4).Value = "Nombres";
            sheet.Cell(1, 5).Value = "Especialidad";
            sheet.Cell(1, 6).Value = "Grado Académico";
            sheet.Cell(1, 7).Value = "Tipo Docente";
            sheet.Cell(1, 8).Value = "Error";

            for (int i = 0; i < errors.Count; i++)
            {
                var err = errors[i];
                sheet.Cell(i + 2, 1).Value = err.Row.DNI;
                sheet.Cell(i + 2, 2).Value = err.Row.ApellidoPaterno;
                sheet.Cell(i + 2, 3).Value = err.Row.ApellidoMaterno;
                sheet.Cell(i + 2, 4).Value = err.Row.Nombres;
                sheet.Cell(i + 2, 5).Value = err.Row.Especialidad;
                sheet.Cell(i + 2, 6).Value = err.Row.Grado;
                sheet.Cell(i + 2, 7).Value = err.Row.Tipo;
                sheet.Cell(i + 2, 8).Value = err.Error;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
