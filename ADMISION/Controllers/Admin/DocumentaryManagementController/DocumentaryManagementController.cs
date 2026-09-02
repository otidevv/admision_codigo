using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace admision.Controllers.Admin.DocumentaryManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/gestion-documental")]
    public class DocumentaryManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IDocumentIssuanceService _issuance;

        public DocumentaryManagementController(AppDbContext context, IDocumentIssuanceService issuance)
        {
            _context = context;
            _issuance = issuance;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? termId, CancellationToken ct)
        {
            var terms = await _context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.IsActive).ThenByDescending(t => t.StartDate)
                .Select(t => new { t.Id, t.Name, t.Year, t.IsActive })
                .ToListAsync(ct);

            ViewBag.Terms = terms;

            Guid? selectedTermId = termId;
            if (!selectedTermId.HasValue)
            {
                var activeTerm = terms.FirstOrDefault(t => t.IsActive);
                if (activeTerm != null) selectedTermId = activeTerm.Id;
            }

            ViewBag.SelectedTermId = selectedTermId;

            if (selectedTermId.HasValue)
            {
                var latestVersion = await _context.ConsolidadoIngresantesVersions
                    .AsNoTracking()
                    .Where(v => v.TermId == selectedTermId.Value && v.IsLatest)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefaultAsync(ct);

                ViewBag.LatestVersion = latestVersion;
            }
            else
            {
                ViewBag.LatestVersion = null;
            }

            return View("~/Pages/Admin/DocumentaryManagement/Index.cshtml");
        }

        [HttpGet("registros")]
        public async Task<IActionResult> GetRecords(Guid versionId, CancellationToken ct)
        {
            var rows = await _issuance.GetIngresantesAsync(versionId, ct);
            return Json(new { count = rows.Count, items = rows });
        }

        [HttpPost("emitir-individual")]
        public async Task<IActionResult> IssueIndividual(Guid recordId, bool watermark = false, CancellationToken ct = default)
        {
            var result = await _issuance.IssueIndividualAsync(recordId, watermark, User.Identity?.Name, ct);
            if (result.NotFound) return NotFound("Registro no encontrado.");

            return File(result.PdfBytes!, "application/pdf", result.FileName!);
        }

        [HttpPost("emitir-masivo")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> IssueBulk([FromBody] BulkEmitRequest request, CancellationToken ct)
        {
            if (request?.RecordIds == null || request.RecordIds.Count == 0)
                return BadRequest("Seleccione al menos un registro.");

            var result = await _issuance.IssueBulkAsync(request.RecordIds, request.Watermark, User.Identity?.Name, ct);
            if (result.NotFound) return NotFound("No se encontraron registros.");

            return File(result.ZipBytes!, "application/zip", result.FileName!);
        }
    }

    public class BulkEmitRequest
    {
        public List<Guid> RecordIds { get; set; } = new();
        public bool Watermark { get; set; }
    }
}
