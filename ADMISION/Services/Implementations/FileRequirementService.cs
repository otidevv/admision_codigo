using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class FileRequirementService : IFileRequirementService
    {
        private readonly AppDbContext _context;

        public FileRequirementService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<FileRequirementListItem>> ListAsync(ListQuery query, CancellationToken ct = default)
        {
            var q = _context.FileRequirementManagements.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(t => EF.Functions.ILike(t.Name, $"%{search}%"));
            }

            // Ordenar sobre la entidad ANTES de proyectar — EF Core 10 no traduce
            // OrderBy sobre records posicionales (positional record constructor).
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "name" => query.IsDescending ? q.OrderByDescending(t => t.Name) : q.OrderBy(t => t.Name),
                "maxfilesizemb" => query.IsDescending ? q.OrderByDescending(t => t.MaxFileSizeMB) : q.OrderBy(t => t.MaxFileSizeMB),
                _ => q.OrderByDescending(t => t.Name)
            };

            var projected = q.Select(t => new FileRequirementListItem(
                t.Id, t.Name, t.Description, t.MaxFileSizeMB, t.FilePathExtencion));

            return await PagedResult<FileRequirementListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public Task<FileRequirementManagement?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _context.FileRequirementManagements.FirstOrDefaultAsync(r => r.Id == id, ct);

        public async Task<FileRequirementManagement> CreateAsync(FileRequirementManagement entity, string actor, CancellationToken ct = default)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity.CreatedBy = actor;
            _context.FileRequirementManagements.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<bool> UpdateAsync(FileRequirementManagement entity, string actor, CancellationToken ct = default)
        {
            var existing = await _context.FileRequirementManagements.AsNoTracking().FirstOrDefaultAsync(r => r.Id == entity.Id, ct);
            if (existing == null) return false;

            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = actor;
            _context.FileRequirementManagements.Update(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<RequirementDeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _context.FileRequirementManagements.FindAsync(new object[] { id }, ct);
            if (entity == null) return RequirementDeleteOutcome.NotFound;

            try
            {
                _context.FileRequirementManagements.Remove(entity);
                await _context.SaveChangesAsync(ct);
                return RequirementDeleteOutcome.Deleted;
            }
            catch (DbUpdateException ex)
            {
                // Distinguir el FK contra TypePostulantRequisite para dar un mensaje útil.
                if (ex.InnerException?.Message.Contains("FK_TypePostulantRequisite") == true)
                    return RequirementDeleteOutcome.UsedByTypePostulant;
                return RequirementDeleteOutcome.HasOtherDependencies;
            }
        }
    }
}
