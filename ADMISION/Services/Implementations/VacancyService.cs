using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class VacancyService : IVacancyService
    {
        private readonly AppDbContext _context;

        public VacancyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VacanciesMatrixViewModel?> BuildMatrixAsync(Guid modalityId, CancellationToken ct = default)
        {
            var modality = await _context.Modalities.AsNoTracking().FirstOrDefaultAsync(m => m.Id == modalityId, ct);
            if (modality == null) return null;

            return new VacanciesMatrixViewModel
            {
                ModalityId = modality.Id,
                ModalityName = modality.Name,
                Careers = await _context.Careers.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(ct),
                TypeModalities = await _context.TypeModalities.AsNoTracking()
                    .Where(tm => tm.ModalityId == modalityId && tm.IsActive)
                    .OrderBy(tm => tm.Name)
                    .ToListAsync(ct),
                Vacancies = await _context.Vacancies
                    .Where(v => v.ModalityId == modalityId)
                    .ToListAsync(ct)
            };
        }

        public async Task SaveMatrixAsync(Guid modalityId, Dictionary<string, int> quantities, string actor, CancellationToken ct = default)
        {
            // Formato esperado: "{careerId}_{typeModalityId|null}".
            if (quantities == null || !quantities.Any()) return;

            var existing = await _context.Vacancies
                .Where(v => v.ModalityId == modalityId)
                .ToListAsync(ct);

            foreach (var kvp in quantities)
            {
                var parts = kvp.Key.Split('_');
                if (parts.Length != 2) continue;

                Guid? typeModalityId = null;
                if (!string.IsNullOrEmpty(parts[1]) && parts[1] != "null"
                    && Guid.TryParse(parts[1], out var parsedTmId))
                {
                    typeModalityId = parsedTmId;
                }

                if (!Guid.TryParse(parts[0], out var careerId)) continue;

                var quantity = kvp.Value;
                var vacancy = existing.FirstOrDefault(v => v.CareerId == careerId && v.TypeModalityId == typeModalityId);

                if (vacancy != null)
                {
                    if (vacancy.Quantity != quantity)
                    {
                        // Ajusta Available según el delta para preservar las plazas ya asignadas.
                        var diff = quantity - vacancy.Quantity;
                        vacancy.Quantity = quantity;
                        vacancy.Available += diff;
                        vacancy.UpdatedAt = DateTimeOffset.UtcNow;
                        vacancy.UpdatedBy = actor;
                        _context.Vacancies.Update(vacancy);
                    }
                }
                else if (quantity > 0)
                {
                    _context.Vacancies.Add(new Vacancies
                    {
                        Id = Guid.NewGuid(),
                        ModalityId = modalityId,
                        CareerId = careerId,
                        TypeModalityId = typeModalityId,
                        Quantity = quantity,
                        Available = quantity,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = actor
                    });
                }
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
