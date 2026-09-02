using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class FacultyService : IFacultyService
    {
        private readonly AppDbContext _context;

        public FacultyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Faculty>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Faculties
                .AsNoTracking()
                .OrderBy(f => f.Name)
                .ToListAsync(ct);
        }

        public Task<Faculty?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Faculties.FirstOrDefaultAsync(f => f.Id == id, ct);
        }

        public async Task<Faculty> CreateAsync(Faculty faculty, string actor, CancellationToken ct = default)
        {
            faculty.Id = Guid.NewGuid();
            faculty.CreatedAt = DateTimeOffset.UtcNow;
            faculty.CreatedBy = actor;
            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync(ct);
            return faculty;
        }

        public async Task<bool> UpdateAsync(Faculty faculty, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Faculties.AsNoTracking().FirstOrDefaultAsync(f => f.Id == faculty.Id, ct);
            if (existing == null) return false;

            faculty.CreatedAt = existing.CreatedAt;
            faculty.CreatedBy = existing.CreatedBy;
            faculty.UpdatedAt = DateTimeOffset.UtcNow;
            faculty.UpdatedBy = actor;

            _context.Faculties.Update(faculty);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var faculty = await _context.Faculties.FindAsync(new object[] { id }, ct);
            if (faculty == null) return DeleteOutcome.NotFound;

            try
            {
                _context.Faculties.Remove(faculty);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }
    }
}
