using ADMISION.ENTITIES.Constants;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADMISION.Controllers.Admin.ImportsController
{
    [Route("admin/importaciones")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    public class ImportsController : Controller
    {
        private readonly IPostulantImportService _import;
        private readonly IImportJobService _jobService;
        private readonly IWebHostEnvironment _env;
        private readonly IBackgroundJobClient _jobs;

        private string TempDir => Path.Combine(_env.WebRootPath, "temp_imports");

        public ImportsController(IPostulantImportService import, IImportJobService jobService,
            IWebHostEnvironment env, IBackgroundJobClient jobs)
        {
            _import = import;
            _jobService = jobService;
            _env = env;
            _jobs = jobs;
        }

        [HttpGet("")]
        public IActionResult Principal()
        {
            return Redirect("/admin/info-postulant");
        }

        [HttpGet("postulantes")]
        public IActionResult Index()
        {
            CleanupTempFiles();
            return View("~/Pages/Admin/Imports/PostulantImport.cshtml");
        }

        [HttpGet("postulantes/template")]
        public IActionResult PostulantsTemplate()
        {
            var bytes = _import.BuildPostulantsTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Postulantes.xlsx");
        }

        [HttpPost("postulantes/preview")]
        [RequestSizeLimit(200_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Preview(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar un archivo Excel.";
                return RedirectToAction(nameof(Index));
            }

            var token = Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(TempDir);
            var tempPath = Path.Combine(TempDir, token + ".xlsx");

            await using (var fs = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(fs, ct);
            }

            using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
            var rows = await _import.PreviewAsync(stream, ct);

            return View("~/Pages/Admin/Imports/PostulantImport.cshtml", new PostulantImportPreview
            {
                Rows = rows,
                FileName = file.FileName,
                TempToken = token
            });
        }

        [HttpPost("postulantes/execute")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute(string token, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Token de importación no válido. Vuelva a cargar el archivo.";
                return RedirectToAction(nameof(Index));
            }

            var tempPath = Path.Combine(TempDir, token + ".xlsx");
            if (!System.IO.File.Exists(tempPath))
            {
                TempData["Error"] = "El archivo temporal no existe. Vuelva a cargar el archivo.";
                return RedirectToAction(nameof(Index));
            }

            var validCount = await GetValidRowCountAsync(tempPath, ct);
            var userName = User.Identity?.Name ?? "Import";

            var job = await _jobService.CreateAsync(
                Path.GetFileName(tempPath),
                validCount,
                token,
                userName);

            var jobId = _jobs.Enqueue<ADMISION.Services.Background.PostulantImportJob>(
                j => j.RunAsync(job.Id, tempPath, userName));

            return View("~/Pages/Admin/Imports/PostulantImport.cshtml", new PostulantImportPreview
            {
                JobId = job.Id,
                FileName = token,
                TempToken = token
            });
        }

        [HttpGet("postulantes/progress/{jobId}")]
        public async Task<IActionResult> Progress(Guid jobId)
        {
            var job = await _jobService.GetByIdAsync(jobId);
            if (job == null)
                return NotFound();

            return Json(new
            {
                job.Id,
                job.Status,
                job.TotalRows,
                job.ProcessedRows,
                job.Inserted,
                job.Skipped,
                job.FailedRows,
                job.ErrorMessage,
                Percent = job.TotalRows > 0
                    ? Math.Round((double)job.ProcessedRows / job.TotalRows * 100, 1)
                    : 0
            });
        }

        [HttpGet("postulantes/result/{jobId}")]
        public async Task<IActionResult> Result(Guid jobId)
        {
            var job = await _jobService.GetByIdAsync(jobId);
            if (job == null)
                return NotFound();

            if (job.Status == "Completed")
            {
                if (job.FailedRows > 0)
                    TempData["Warning"] = $"Importación completada con {job.FailedRows} error(es). {job.Inserted} insertado(s), {job.Skipped} omitido(s).";
                else
                    TempData["Success"] = $"Importación exitosa. {job.Inserted} postulante(s) importado(s).";

                try { System.IO.File.Delete(Path.Combine(TempDir, job.TempToken + ".xlsx")); } catch { }
            }
            else if (job.Status == "Failed")
            {
                TempData["Error"] = $"Error en la importación: {job.ErrorMessage}";
            }
            else
            {
                TempData["Info"] = "La importación aún está en proceso.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("admission-results")]
        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        public IActionResult AdmissionResults()
        {
            return RedirectToAction("Index", "AdmissionResultImport");
        }

        private async Task<int> GetValidRowCountAsync(string tempPath, CancellationToken ct)
        {
            using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
            var rows = await _import.PreviewAsync(stream, ct);
            return rows.Count(r => r.IsValid);
        }

        private void CleanupTempFiles()
        {
            try
            {
                if (!Directory.Exists(TempDir)) return;
                foreach (var f in Directory.GetFiles(TempDir, "*.xlsx"))
                {
                    var age = DateTime.UtcNow - System.IO.File.GetCreationTimeUtc(f);
                    if (age.TotalMinutes > 30)
                        try { System.IO.File.Delete(f); } catch { }
                }
            }
            catch { /* ignore */ }
        }
    }
}
