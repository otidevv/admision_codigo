using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ScoringProfileService : IScoringProfileService
    {
        private readonly AppDbContext _context;

        public ScoringProfileService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ScoringProfileListItem>> ListAsync(ScoringProfileListQuery query, CancellationToken ct = default)
        {
            var q = _context.ScoringProfiles
                .AsNoTracking()
                .Include(p => p.Term)
                .Include(p => p.Modality)
                .Include(p => p.Ranges)
                .AsQueryable();

            if (query.TermId.HasValue && query.TermId.Value != Guid.Empty)
                q = q.Where(p => p.TermId == query.TermId.Value);
            if (query.ModalityId.HasValue && query.ModalityId.Value != Guid.Empty)
                q = q.Where(p => p.ModalityId == query.ModalityId.Value);
            if (query.TypeModalityId.HasValue && query.TypeModalityId.Value != Guid.Empty)
                q = q.Where(p => p.TypeModalityId == query.TypeModalityId.Value);
            if (query.IsWeighted.HasValue)
                q = q.Where(p => p.IsWeighted == query.IsWeighted.Value);
            if (query.IsActive.HasValue)
                q = q.Where(p => p.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
            }

            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "name" => query.IsDescending ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
                "mode" => query.IsDescending ? q.OrderByDescending(p => p.IsWeighted) : q.OrderBy(p => p.IsWeighted),
                "term" => query.IsDescending ? q.OrderByDescending(p => p.Term!.Name) : q.OrderBy(p => p.Term!.Name),
                _ => q.OrderByDescending(p => p.CreatedAt)
            };

            var projected = q.Select(p => new ScoringProfileListItem
            {
                Id = p.Id,
                Name = p.Name,
                IsWeighted = p.IsWeighted,
                PuntosCorrecta = p.PuntosCorrecta,
                PuntosBlanco = p.PuntosBlanco,
                PuntosIncorrecta = p.PuntosIncorrecta,
                IsActive = p.IsActive,
                RangeCount = p.Ranges.Count,
                TermName = p.Term != null ? p.Term.Name : null,
                ModalityName = p.Modality != null ? p.Modality.Name : null
            });

            return await PagedResult<ScoringProfileListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public async Task<ScoringProfileDetail?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.ScoringProfiles
                .AsNoTracking()
                .Include(p => p.Term)
                .Include(p => p.Modality)
                .Include(p => p.TypeModality)
                .Include(p => p.Career)
                .Include(p => p.Ranges)
                .Where(p => p.Id == id)
                .Select(p => new ScoringProfileDetail
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    IsWeighted = p.IsWeighted,
                    PuntosCorrecta = p.PuntosCorrecta,
                    PuntosBlanco = p.PuntosBlanco,
                    PuntosIncorrecta = p.PuntosIncorrecta,
                    NotaMinimaIngreso = p.NotaMinimaIngreso,
                    AplicarVigesimal = p.AplicarVigesimal,
                    ManejoAnuladas = p.ManejoAnuladas,
                    TermId = p.TermId,
                    ModalityId = p.ModalityId,
                    TypeModalityId = p.TypeModalityId,
                    CareerId = p.CareerId,
                    IsActive = p.IsActive,
                    TermName = p.Term != null ? p.Term.Name : null,
                    ModalityName = p.Modality != null ? p.Modality.Name : null,
                    TypeModalityName = p.TypeModality != null ? p.TypeModality.Name : null,
                    CareerName = p.Career != null ? p.Career.Name : null,
                    Ranges = p.Ranges
                        .OrderBy(r => r.DisplayOrder)
                        .Select(r => new ScoringProfileRangeDetail
                        {
                            Id = r.Id,
                            FromQuestion = r.FromQuestion,
                            ToQuestion = r.ToQuestion,
                            PuntosCorrecta = r.PuntosCorrecta,
                            DisplayOrder = r.DisplayOrder
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<SaveResult> CreateAsync(ScoringProfile profile, IReadOnlyList<ScoringProfileRange> ranges, string actor, CancellationToken ct = default)
        {
            var errors = Validate(profile, ranges);
            if (errors.Count > 0) return SaveResult.Invalid(errors);

            profile.Id = Guid.NewGuid();
            profile.CreatedAt = DateTimeOffset.UtcNow;
            profile.CreatedBy = actor;
            profile.UpdatedAt = null;
            profile.UpdatedBy = null;

            _context.ScoringProfiles.Add(profile);

            var now = DateTimeOffset.UtcNow;
            var order = 0;
            foreach (var r in ranges)
            {
                profile.Ranges.Add(new ScoringProfileRange
                {
                    Id = Guid.NewGuid(),
                    ScoringProfileId = profile.Id,
                    FromQuestion = r.FromQuestion,
                    ToQuestion = r.ToQuestion,
                    PuntosCorrecta = r.PuntosCorrecta,
                    DisplayOrder = order++,
                    CreatedAt = now,
                    CreatedBy = actor
                });
            }

            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<SaveResult> UpdateAsync(ScoringProfile profile, IReadOnlyList<ScoringProfileRange> ranges, string actor, CancellationToken ct = default)
        {
            var errors = Validate(profile, ranges);
            if (errors.Count > 0) return SaveResult.Invalid(errors);

            var existing = await _context.ScoringProfiles
                .Include(p => p.Ranges)
                .FirstOrDefaultAsync(p => p.Id == profile.Id, ct);

            if (existing == null) return SaveResult.NotFoundResult();

            existing.Name = profile.Name;
            existing.Description = profile.Description;
            existing.IsWeighted = profile.IsWeighted;
            existing.PuntosCorrecta = profile.PuntosCorrecta;
            existing.PuntosBlanco = profile.PuntosBlanco;
            existing.PuntosIncorrecta = profile.PuntosIncorrecta;
            existing.NotaMinimaIngreso = profile.NotaMinimaIngreso;
            existing.AplicarVigesimal = profile.AplicarVigesimal;
            existing.ManejoAnuladas = profile.ManejoAnuladas;
            existing.TermId = profile.TermId;
            existing.ModalityId = profile.ModalityId;
            existing.TypeModalityId = profile.TypeModalityId;
            existing.CareerId = profile.CareerId;
            existing.IsActive = profile.IsActive;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = actor;

            _context.ScoringProfileRanges.RemoveRange(existing.Ranges);

            var now = DateTimeOffset.UtcNow;
            var order = 0;
            foreach (var r in ranges)
            {
                existing.Ranges.Add(new ScoringProfileRange
                {
                    Id = Guid.NewGuid(),
                    ScoringProfileId = existing.Id,
                    FromQuestion = r.FromQuestion,
                    ToQuestion = r.ToQuestion,
                    PuntosCorrecta = r.PuntosCorrecta,
                    DisplayOrder = order++,
                    CreatedAt = now,
                    CreatedBy = actor
                });
            }

            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var item = await _context.ScoringProfiles.FindAsync(new object[] { id }, ct);
            if (item == null) return DeleteOutcome.NotFound;

            try
            {
                _context.ScoringProfiles.Remove(item);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        private static List<ValidationError> Validate(ScoringProfile profile, IReadOnlyList<ScoringProfileRange> ranges)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(profile.Name))
                errors.Add(new ValidationError("Name", "El nombre del perfil es obligatorio."));
            else if (profile.Name.Trim().Length > 180)
                errors.Add(new ValidationError("Name", "El nombre no puede superar los 180 caracteres."));

            if (profile.PuntosCorrecta < 0)
                errors.Add(new ValidationError("PuntosCorrecta", "Los puntos por correcta no pueden ser negativos."));
            if (profile.PuntosBlanco < 0)
                errors.Add(new ValidationError("PuntosBlanco", "Los puntos por blanco no pueden ser negativos."));
            if (profile.PuntosIncorrecta < 0)
                errors.Add(new ValidationError("PuntosIncorrecta", "Los puntos por incorrecta no pueden ser negativos."));
            if (profile.NotaMinimaIngreso < 0)
                errors.Add(new ValidationError("NotaMinimaIngreso", "La nota mínima no puede ser negativa."));

            if (profile.IsWeighted)
            {
                if (ranges == null || ranges.Count == 0)
                {
                    errors.Add(new ValidationError("", "Para la calificación ponderada debes registrar al menos un rango de preguntas."));
                    return errors;
                }

                var sorted = ranges
                    .Where(r => r.FromQuestion >= 1 && r.ToQuestion >= r.FromQuestion)
                    .OrderBy(r => r.FromQuestion)
                    .ToList();

                var invalidCount = ranges.Count(r => r.FromQuestion < 1 || r.ToQuestion < r.FromQuestion || r.PuntosCorrecta < 0);
                if (invalidCount > 0)
                    errors.Add(new ValidationError("", "Hay rangos inválidos: revisa que 'desde' sea ≥ 1, 'hasta' sea ≥ 'desde' y los puntos no sean negativos."));

                // Detección de solapamientos entre rangos ordenados.
                for (var i = 1; i < sorted.Count; i++)
                {
                    if (sorted[i].FromQuestion <= sorted[i - 1].ToQuestion)
                    {
                        errors.Add(new ValidationError("", "Los rangos de preguntas no pueden solaparse."));
                        break;
                    }
                }
            }

            return errors;
        }
    }
}
