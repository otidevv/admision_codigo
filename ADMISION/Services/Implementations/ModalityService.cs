using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ModalityService : IModalityService
    {
        private readonly AppDbContext _context;

        public ModalityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ModalityListItem>> ListAsync(ModalityListQuery query, CancellationToken ct = default)
        {
            var q = _context.Modalities
                .AsNoTracking()
                .Include(m => m.Term)
                .AsQueryable();

            if (query.TermId.HasValue && query.TermId.Value != Guid.Empty)
            {
                q = q.Where(m => m.TermId == query.TermId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(m => EF.Functions.ILike(m.Name, $"%{search}%"));
            }

            // Ordenar sobre la entidad ANTES de proyectar — EF Core 10 no traduce
            // OrderBy sobre records posicionales (positional record constructor).
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "name" => query.IsDescending ? q.OrderByDescending(m => m.Name) : q.OrderBy(m => m.Name),
                "term" => query.IsDescending ? q.OrderByDescending(m => m.Term!.Name) : q.OrderBy(m => m.Term!.Name),
                "isactive" => query.IsDescending ? q.OrderByDescending(m => m.IsActive) : q.OrderBy(m => m.IsActive),
                "orden" => query.IsDescending ? q.OrderByDescending(m => m.Orden) : q.OrderBy(m => m.Orden),
                _ => q.OrderByDescending(m => m.Name)
            };

            var projected = q.Select(m => new ModalityListItem(
                m.Id,
                m.Name,
                m.Description,
                m.IsActive,
                m.Orden,
                m.IsCepreExam,
                m.RequiresProfilePhoto,
                m.IsMockExam,
                m.RequiresEducationalLevel,
                m.RequiresGrade,
                m.StartDate,
                m.EndDate,
                m.StartTime,
                m.EndTime,
                m.Term != null ? m.Term.Name : null));

            return await PagedResult<ModalityListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public Task<Modality?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Modalities.FirstOrDefaultAsync(m => m.Id == id, ct);
        }

        public async Task<SaveResult> CreateAsync(Modality modality, string actor, CancellationToken ct = default)
        {
            var errors = new List<ValidationError>();
            errors.AddRange(await ValidateTermBoundsAsync(modality, ct));
            errors.AddRange(await ValidateDuplicateNameAsync(modality, currentModalityId: null, ct));
            errors.AddRange(await ValidateStartingCodeAsync(modality, currentModalityId: null, ct));
            if (errors.Any()) return SaveResult.Invalid(errors);

            modality.Id = Guid.NewGuid();
            modality.CreatedAt = DateTimeOffset.UtcNow;
            modality.CreatedBy = actor;
            _context.Modalities.Add(modality);
            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<SaveResult> UpdateAsync(Modality modality, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Modalities.AsNoTracking().FirstOrDefaultAsync(m => m.Id == modality.Id, ct);
            if (existing == null) return SaveResult.NotFoundResult();

            var errors = new List<ValidationError>();
            errors.AddRange(await ValidateTermBoundsAsync(modality, ct));
            errors.AddRange(await ValidateDuplicateNameAsync(modality, currentModalityId: modality.Id, ct));
            errors.AddRange(await ValidateStartingCodeAsync(modality, currentModalityId: modality.Id, ct));
            if (errors.Any()) return SaveResult.Invalid(errors);

            modality.CreatedAt = existing.CreatedAt;
            modality.CreatedBy = existing.CreatedBy;
            modality.UpdatedAt = DateTimeOffset.UtcNow;
            modality.UpdatedBy = actor;

            _context.Modalities.Update(modality);
            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var modality = await _context.Modalities.FindAsync(new object[] { id }, ct);
            if (modality == null) return DeleteOutcome.NotFound;

            try
            {
                _context.Modalities.Remove(modality);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        public async Task<IReadOnlyList<NamedOption>> GetByTermAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.Modalities
                .AsNoTracking()
                .Where(m => m.TermId == termId)
                .OrderBy(m => m.Name)
                .Select(m => new NamedOption(m.Id, m.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Guid>> GetCareerIdsAsync(Guid modalityId, CancellationToken ct = default)
        {
            return await _context.ModalityCareers
                .AsNoTracking()
                .Where(mc => mc.ModalityId == modalityId)
                .Select(mc => mc.CareerId)
                .ToListAsync(ct);
        }

        public async Task SaveCareerAssociationsAsync(Guid modalityId, List<Guid> careerIds, CancellationToken ct = default)
        {
            var existing = await _context.ModalityCareers
                .Where(mc => mc.ModalityId == modalityId)
                .ToListAsync(ct);

            _context.ModalityCareers.RemoveRange(existing);

            if (careerIds != null && careerIds.Any())
            {
                var newAssocs = careerIds
                    .Distinct()
                    .Select(careerId => new ModalityCareer
                    {
                        Id = Guid.NewGuid(),
                        ModalityId = modalityId,
                        CareerId = careerId
                    });
                _context.ModalityCareers.AddRange(newAssocs);
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<Modality>> GetEntitiesByTermAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.Modalities
                .AsNoTracking()
                .Where(m => m.TermId == termId)
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
                .ToListAsync(ct);
        }

        // ───────── Validación: nombre duplicado en el mismo periodo ─────────
        private async Task<List<ValidationError>> ValidateDuplicateNameAsync(Modality modality, Guid? currentModalityId, CancellationToken ct)
        {
            var errors = new List<ValidationError>();

            var exists = await _context.Modalities
                .AnyAsync(m => m.TermId == modality.TermId
                            && EF.Functions.ILike(m.Name, modality.Name)
                            && m.Id != currentModalityId, ct);

            if (exists)
            {
                errors.Add(new ValidationError(nameof(Modality.Name),
                    "Ya existe una modalidad con el mismo nombre en este periodo."));
            }

            return errors;
        }

        // ───────── Validación: fechas dentro del periodo académico ─────────
        // Regla: StartDate, EndDate, ExamDate y ResultsPublicationDate de la modalidad
        // deben caer dentro del rango [Term.StartDate, Term.EndDate]. Además StartDate
        // no puede ser posterior a EndDate.
        private async Task<List<ValidationError>> ValidateTermBoundsAsync(Modality modality, CancellationToken ct)
        {
            var errors = new List<ValidationError>();

            var term = await _context.Terms.AsNoTracking()
                .Where(t => t.Id == modality.TermId)
                .Select(t => new { t.Name, t.StartDate, t.EndDate })
                .FirstOrDefaultAsync(ct);

            if (term == null)
            {
                errors.Add(new ValidationError(nameof(Modality.TermId),
                    "El periodo académico seleccionado no existe."));
                return errors;
            }

            // Coherencia interna: la fecha de inicio no debe pasar a la de fin.
            if (modality.StartDate > modality.EndDate)
            {
                errors.Add(new ValidationError(nameof(Modality.EndDate),
                    "La fecha de fin no puede ser anterior a la fecha de inicio."));
            }
            // Y si caen el mismo día, la hora de cierre debe ser posterior a la de apertura.
            else if (modality.StartDate == modality.EndDate
                     && modality.StartTime >= modality.EndTime)
            {
                errors.Add(new ValidationError(nameof(Modality.EndTime),
                    "La hora de cierre debe ser posterior a la hora de inicio."));
            }

            var rangeLabel = $"«{term.Name}» ({term.StartDate:dd/MM/yyyy} – {term.EndDate:dd/MM/yyyy})";

            void CheckInRange(DateOnly value, string field, string label)
            {
                if (value < term.StartDate || value > term.EndDate)
                {
                    errors.Add(new ValidationError(field,
                        $"{label} debe estar dentro del periodo {rangeLabel}."));
                }
            }

            CheckInRange(modality.StartDate, nameof(Modality.StartDate), "La fecha de inicio");
            CheckInRange(modality.EndDate, nameof(Modality.EndDate), "La fecha de fin");
            if (modality.ExamDate.HasValue)
                CheckInRange(modality.ExamDate.Value, nameof(Modality.ExamDate), "La fecha del examen");
            if (modality.ResultsPublicationDate.HasValue)
                CheckInRange(modality.ResultsPublicationDate.Value, nameof(Modality.ResultsPublicationDate), "La fecha de publicación de resultados");

            return errors;
        }

        // ───────── Validación del correlativo ─────────
        private async Task<List<ValidationError>> ValidateStartingCodeAsync(Modality modality, Guid? currentModalityId, CancellationToken ct)
        {
            var errors = new List<ValidationError>();
            if (string.IsNullOrWhiteSpace(modality.StartingCode))
                return errors; // opcional

            var code = modality.StartingCode.Trim();

            if (!code.All(char.IsDigit))
            {
                errors.Add(new ValidationError(nameof(Modality.StartingCode),
                    "El número inicial debe contener solo dígitos (se permiten ceros a la izquierda)."));
                return errors;
            }

            if (!long.TryParse(code, out var numeric))
            {
                errors.Add(new ValidationError(nameof(Modality.StartingCode),
                    "El número inicial no es válido."));
                return errors;
            }

            // Normalizamos la forma guardada
            modality.StartingCode = code;

            // Unicidad dentro del mismo periodo entre modalidades activas (compara por valor numérico).
            var siblings = await _context.Modalities.AsNoTracking()
                .Where(m => m.TermId == modality.TermId
                            && m.IsActive
                            && m.Id != currentModalityId
                            && m.StartingCode != null)
                .Select(m => new { m.Id, m.Name, m.StartingCode })
                .ToListAsync(ct);

            var clash = siblings.FirstOrDefault(s =>
                long.TryParse(s.StartingCode, out var other) && other == numeric);

            if (clash != null)
            {
                errors.Add(new ValidationError(nameof(Modality.StartingCode),
                    $"Ya existe otra modalidad activa en el mismo periodo con el mismo número inicial ({clash.Name})."));
            }

            return errors;
        }
    }
}
