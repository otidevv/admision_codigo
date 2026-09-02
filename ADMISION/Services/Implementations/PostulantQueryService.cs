using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class PostulantQueryService : IPostulantQueryService
    {
        private readonly AppDbContext _context;

        public PostulantQueryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<PostulantInscriptionListItem>> ListAsync(PostulantInscriptionListQuery query, CancellationToken ct = default)
        {
            var q = _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Postulant).ThenInclude(p => p!.User)
                .Include(i => i.Career).ThenInclude(c => c!.Faculty)
                .Include(i => i.Modality).ThenInclude(m => m!.Term)
                .Include(i => i.TypeModality)
                .Include(i => i.TypePostulantInscription)
                .AsQueryable();

            if (query.AreaId.HasValue)
            {
                q = q.Where(i => _context.TematicAreaCareers.Any(tac =>
                    tac.TematicAreaId == query.AreaId.Value &&
                    tac.CareerId == i.CareerId &&
                    tac.TermId == i.Modality!.TermId));
            }

            if (query.TermId.HasValue) q = q.Where(i => i.Modality!.TermId == query.TermId.Value);
            if (query.CareerId.HasValue) q = q.Where(i => i.CareerId == query.CareerId.Value);
            if (query.FacultyId.HasValue) q = q.Where(i => i.Career!.FacultyId == query.FacultyId.Value);
            if (query.ModalityId.HasValue) q = q.Where(i => i.ModalityId == query.ModalityId.Value);
            if (query.TypeModalityId.HasValue) q = q.Where(i => i.TypeModalityId == query.TypeModalityId.Value);
            if (query.TypePostulantId.HasValue) q = q.Where(i => i.TypePostulantInscriptionId == query.TypePostulantId.Value);
            if (!string.IsNullOrEmpty(query.State)) q = q.Where(i => i.State == query.State);

            if (!string.IsNullOrEmpty(query.Search))
            {
                var search = query.Search.ToLower();
                q = q.Where(i =>
                    i.CodePostulant.ToLower().Contains(search) ||
                    (i.Postulant != null && i.Postulant.User != null && i.Postulant.User.FullName.ToLower().Contains(search)) ||
                    (i.Postulant != null && i.Postulant.User != null && i.Postulant.User.Document.ToLower().Contains(search)));
            }

            // Ordenamiento sobre el IQueryable original (antes de proyectar) por las navegaciones.
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "code" => query.IsDescending ? q.OrderByDescending(i => i.CodePostulant) : q.OrderBy(i => i.CodePostulant),
                "fullname" => query.IsDescending ? q.OrderByDescending(i => i.Postulant!.User!.FullName) : q.OrderBy(i => i.Postulant!.User!.FullName),
                "createdat" => query.IsDescending ? q.OrderByDescending(i => i.CreatedAt) : q.OrderBy(i => i.CreatedAt),
                "career" => query.IsDescending ? q.OrderByDescending(i => i.Career!.Name) : q.OrderBy(i => i.Career!.Name),
                "state" => query.IsDescending ? q.OrderByDescending(i => i.State) : q.OrderBy(i => i.State),
                _ => q.OrderByDescending(i => i.CreatedAt)
            };

            var projected = q.Select(i => new PostulantInscriptionListItem(
                i.Id,
                i.PostulantId,
                i.CodePostulant,
                i.CreatedAt,
                i.State,
                i.Postulant != null && i.Postulant.User != null ? i.Postulant.User.FullName : null,
                i.Postulant != null && i.Postulant.User != null ? i.Postulant.User.Document : null,
                i.Postulant != null && i.Postulant.User != null ? i.Postulant.User.DocumentType : null,
                i.Career != null ? i.Career.Name : null,
                i.Career != null ? i.Career.TematicArea : null,
                i.Modality != null ? i.Modality.Name : null,
                i.TypeModality != null ? i.TypeModality.Name : null));

            return await PagedResult<PostulantInscriptionListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public async Task<PostulantInscriptionEditData?> GetForEditAsync(Guid id, CancellationToken ct = default)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Postulant).ThenInclude(p => p!.User)
                .Include(i => i.Career)
                .Include(i => i.Modality).ThenInclude(m => m!.Term)
                .Include(i => i.School).ThenInclude(s => s!.Distrit).ThenInclude(d => d!.Province).ThenInclude(p => p!.Department)
                .Include(i => i.Country)
                .Include(i => i.Distrit).ThenInclude(d => d!.Province).ThenInclude(p => p!.Department)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (inscription == null) return null;

            string? tematicAreaCode = null;
            var termId = inscription.Modality?.TermId;
            if (termId.HasValue)
            {
                tematicAreaCode = await _context.TematicAreaCareers
                    .AsNoTracking()
                    .Where(tac => tac.CareerId == inscription.CareerId && tac.TermId == termId.Value)
                    .Select(tac => tac.TematicArea!.Code)
                    .FirstOrDefaultAsync(ct);
            }

            return new PostulantInscriptionEditData(inscription, tematicAreaCode);
        }

        public async Task<SaveResult> UpdateAsync(Guid id, Inscription model, string actor, CancellationToken ct = default)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Postulant).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (inscription == null) return SaveResult.NotFoundResult();

            inscription.State = model.State;
            inscription.CareerId = model.CareerId;
            inscription.ModalityId = model.ModalityId;
            inscription.CountryId = model.CountryId;
            inscription.DistritId = model.DistritId;
            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.UpdatedBy = actor;

            if (inscription.Postulant?.User != null && model.Postulant?.User != null)
            {
                var u = inscription.Postulant.User;
                u.Name = model.Postulant.User.Name;
                u.FirstNameFather = model.Postulant.User.FirstNameFather;
                u.FirstNameMother = model.Postulant.User.FirstNameMother;
                u.Email = model.Postulant.User.Email;
                u.PhoneNumber = model.Postulant.User.PhoneNumber;
                u.UpdatedAt = DateTimeOffset.UtcNow;
                u.UpdatedBy = actor;
            }

            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }
    }
}
