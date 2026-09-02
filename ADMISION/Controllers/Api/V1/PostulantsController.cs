using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Controllers.Api.V1
{
    [Route("api/v1/postulants")]
    [ApiController]
    [Authorize(Policy = "ApiConsumer")]
    public class PostulantsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConsolidadoConsultaService _consulta;

        public PostulantsController(AppDbContext context, IConsolidadoConsultaService consulta)
        {
            _context = context;
            _consulta = consulta;
        }

        [HttpGet("consolidado")]
        public async Task<IActionResult> GetConsolidado(CancellationToken ct)
        {
            var version = await _consulta.GetLatestVersionAsync(ct);
            if (version == null)
                return NotFound(new { error = "No se encontró un consolidado activo para el período vigente." });

            var records = await _consulta.GetRecordsByVersionAsync(version.Id, ct);

            return Ok(new
            {
                version = new
                {
                    version.VersionNumber,
                    version.IsLatest,
                    version.RecordCount,
                    version.CreatedAt
                },
                total = records.Count,
                items = records
            });
        }
    }
}
