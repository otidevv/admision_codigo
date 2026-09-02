using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ADMISION.Controllers.Public
{
    /// <summary>
    /// Endpoints públicos relacionados con la Constancia de Inscripción:
    /// descarga del PDF y verificación pública del QR.
    /// </summary>
    [Route("")]
    public class InscriptionDocumentController : Controller
    {
        private readonly IInscriptionDocumentService _docs;

        public InscriptionDocumentController(IInscriptionDocumentService docs) { _docs = docs; }

        [HttpGet("~/inscripcion/{id:guid}/constancia")]
        [EnableRateLimiting("public-lookup")]
        public async Task<IActionResult> Download(Guid id, [FromQuery] bool inline, CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _docs.BuildConstanciaAsync(id, baseUrl, ct: ct);
            if (result == null) return NotFound();

            // Por defecto fuerza la descarga (attachment). Pasar ?inline=true para visualizar
            // en el visor del navegador (p. ej. cuando se escanea el QR y se quiere ver la
            // constancia desde la web de verificación).
            var disposition = inline ? "inline" : "attachment";
            Response.Headers["Content-Disposition"] = $"{disposition}; filename=\"{result.FileName}\"";
            return File(result.PdfBytes, "application/pdf");
        }

        [HttpGet("~/verificar/{code}")]
        [EnableRateLimiting("public-lookup")]
        public async Task<IActionResult> Verify(string code, CancellationToken ct)
        {
            var vm = await _docs.GetVerificationAsync(code, ct);
            ViewBag.Code = code;
            return View("~/Pages/Public/VerifyConstancia.cshtml", vm);
        }
    }
}
