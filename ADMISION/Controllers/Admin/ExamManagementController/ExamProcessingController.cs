using ADMISION.ENTITIES.Constants;
using ADMISION.Extensions;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ADMISION.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/processing")]
    public class ExamProcessingController : Controller
    {
        private const string ReportSessionKey = "ExternalProcessReport";
        private readonly IExamProcessingService _service;
        private readonly IScoringProfileService _profiles;
        private readonly ILogger<ExamProcessingController> _logger;

        public ExamProcessingController(
            IExamProcessingService service,
            IScoringProfileService profiles,
            ILogger<ExamProcessingController> logger)
        {
            _service = service;
            _profiles = profiles;
            _logger = logger;
        }

        [HttpGet("external")]
        public async Task<IActionResult> External(CancellationToken ct)
        {
            var list = await _profiles.ListAsync(new ScoringProfileListQuery
            {
                IsActive = true,
                Page = 1,
                PageSize = 200,
                SortBy = "name"
            }, ct);

            return View("~/Pages/Admin/ExamManagement/ExamProcessing/External.cshtml",
                new ExternalProcessFormModel
                {
                    Profiles = list.Items,
                    Report = GetStoredReport()
                });
        }

        [HttpGet("external/template")]
        public IActionResult Template()
        {
            var bytes = _service.BuildPostulantsTemplate();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Plantilla_BD_Postulantes.xlsx");
        }

        [HttpGet("external/download")]
        public IActionResult Download()
        {
            var report = GetStoredReport();
            if (report == null)
            {
                TempData["Error"] = "No hay resultados procesados para descargar. Procesa primero los archivos.";
                return RedirectToAction(nameof(External));
            }

            var bytes = _service.BuildExternalExcel(report.Data, report.ProfileName, report.Titulo);
            var filename = $"Resultados_Externo_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        [HttpPost("external")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> ExternalProcess(
            IFormFile? keyFile,
            IFormFile? answersFile,
            IFormFile? identificationFile,
            IFormFile? bdFile,
            [FromForm] ExternalProcessFormModel form,
            CancellationToken ct)
        {
            if (keyFile == null || keyFile.Length == 0)
            {
                TempData["Error"] = "Debe subir el archivo de clave.";
                return RedirectToAction(nameof(External));
            }
            if (answersFile == null || answersFile.Length == 0)
            {
                TempData["Error"] = "Debe subir el archivo de respuestas de postulantes.";
                return RedirectToAction(nameof(External));
            }
            if (!form.ScoringProfileId.HasValue || form.ScoringProfileId.Value == Guid.Empty)
            {
                TempData["Error"] = "Debe seleccionar un perfil de calificación.";
                return RedirectToAction(nameof(External));
            }

            var profile = await _profiles.GetByIdAsync(form.ScoringProfileId.Value, ct);
            if (profile == null || !profile.IsActive)
            {
                TempData["Error"] = "El perfil de calificación seleccionado no existe o está inactivo.";
                return RedirectToAction(nameof(External));
            }

            var parameters = new ExternalScoringParameters
            {
                PuntosCorrecta = profile.PuntosCorrecta,
                PuntosBlanco = profile.PuntosBlanco,
                PuntosIncorrecta = profile.PuntosIncorrecta,
                NotaMinimaIngreso = profile.NotaMinimaIngreso,
                AplicarVigesimal = profile.AplicarVigesimal,
                ManejoAnuladas = profile.ManejoAnuladas,
                WeightedRanges = profile.IsWeighted
                    ? profile.Ranges
                        .OrderBy(r => r.FromQuestion)
                        .Select(r => new ExternalScoringRange
                        {
                            FromQuestion = r.FromQuestion,
                            ToQuestion = r.ToQuestion,
                            PuntosCorrecta = r.PuntosCorrecta
                        })
                        .ToList()
                    : null
            };

            using var keyStream = keyFile.OpenReadStream();
            using var ansStream = answersFile.OpenReadStream();
            Stream? identStream = identificationFile != null && identificationFile.Length > 0
                ? identificationFile.OpenReadStream()
                : null;
            Stream? bdStream = bdFile != null && bdFile.Length > 0 ? bdFile.OpenReadStream() : null;

            try
            {
                var data = _service.ProcessExternal(keyStream, ansStream, identStream, bdStream, bdFile?.FileName, parameters);
                if (data.Errors.Any())
                {
                    TempData["Error"] = string.Join(" · ", data.Errors);
                    return RedirectToAction(nameof(External));
                }

                HttpContext.Session.SetString(ReportSessionKey, JsonSerializer.Serialize(new ExternalProcessReport
                {
                    Titulo = form.Titulo ?? "",
                    ProfileName = profile.Name,
                    Data = data
                }));

                return RedirectToAction(nameof(External));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return RedirectToAction(nameof(External));
            }
            finally
            {
                identStream?.Dispose();
                bdStream?.Dispose();
            }
        }

        private ExternalProcessReport? GetStoredReport()
        {
            var json = HttpContext.Session.GetString(ReportSessionKey);
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<ExternalProcessReport>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
