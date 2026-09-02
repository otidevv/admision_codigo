using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Infrastructure;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ClassroomService : IClassroomService
    {
        private readonly AppDbContext _context;

        public ClassroomService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Classroom>> GetAllAsync(Guid? pavilionId, CancellationToken ct = default)
        {
            var q = _context.Classrooms
                .AsNoTracking()
                .Include(c => c.Pavilion)
                .AsQueryable();

            if (pavilionId.HasValue && pavilionId.Value != Guid.Empty)
                q = q.Where(c => c.PavilionId == pavilionId.Value);

            return await q
                .OrderBy(c => c.Pavilion!.Code)
                .ThenBy(c => c.Floor)
                .ThenBy(c => c.Name)
                .ToListAsync(ct);
        }

        public Task<Classroom?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<Classroom> CreateAsync(Classroom classroom, string actor, CancellationToken ct = default)
        {
            classroom.Id = Guid.NewGuid();
            classroom.CreatedAt = DateTimeOffset.UtcNow;
            classroom.CreatedBy = actor;
            _context.Classrooms.Add(classroom);
            await _context.SaveChangesAsync(ct);
            return classroom;
        }

        public async Task<bool> UpdateAsync(Classroom classroom, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Classrooms.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classroom.Id, ct);
            if (existing == null) return false;

            classroom.CreatedAt = existing.CreatedAt;
            classroom.CreatedBy = existing.CreatedBy;
            classroom.UpdatedAt = DateTimeOffset.UtcNow;
            classroom.UpdatedBy = actor;
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var classroom = await _context.Classrooms.FindAsync(new object[] { id }, ct);
            if (classroom == null) return DeleteOutcome.NotFound;

            try
            {
                _context.Classrooms.Remove(classroom);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        public async Task<IReadOnlyList<PavilionOption>> GetActivePavilionsAsync(CancellationToken ct = default)
        {
            return await _context.Pavilions
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Code)
                .Select(p => new PavilionOption(p.Id, p.Name, p.Code))
                .ToListAsync(ct);
        }
    }
}
