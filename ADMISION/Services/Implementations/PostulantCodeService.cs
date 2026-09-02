using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class PostulantCodeService : IPostulantCodeService
    {
        private readonly AppDbContext _context;

        public PostulantCodeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateNextAsync(Guid modalityId, string fallbackDocumentNumber)
        {
            // Serializa la generación de código por modalidad dentro de la transacción actual.
            // Si otra transacción ya está generando un código para la misma modalidad,
            // esta se bloquea hasta que la primera haga commit o rollback.
            await _context.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(hashtext({0}))", modalityId.ToString());

            var modality = await _context.Modalities.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == modalityId);

            if (modality == null || string.IsNullOrWhiteSpace(modality.StartingCode)
                || !long.TryParse(modality.StartingCode, out var start))
            {
                // Sin configuración de correlativo → fallback al formato legacy.
                return "P-" + fallbackDocumentNumber;
            }

            int padding = modality.StartingCode.Length;

            // Busco el máximo código numérico existente en esta modalidad.
            // Cargo solo los códigos (que son pocos en tamaño) y parseo en memoria
            // para ignorar códigos no numéricos legacy ("P-xxxx").
            var existingCodes = await _context.Inscriptions
                .AsNoTracking()
                .Where(i => i.ModalityId == modalityId)
                .Select(i => i.CodePostulant)
                .ToListAsync();

            long? maxCurrent = null;
            foreach (var code in existingCodes)
            {
                if (long.TryParse(code, out var n))
                {
                    if (!maxCurrent.HasValue || n > maxCurrent.Value) maxCurrent = n;
                }
            }

            long next = maxCurrent.HasValue ? maxCurrent.Value + 1 : start;
            if (next < start) next = start; // por si el max quedó por debajo

            return next.ToString().PadLeft(padding, '0');
        }
    }
}
