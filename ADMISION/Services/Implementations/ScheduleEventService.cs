using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ScheduleEventService : IScheduleEventService
    {
        private readonly AppDbContext _context;

        public ScheduleEventService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ScheduleEvent>> GetByTermAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.ScheduleEvents
                .AsNoTracking()
                .Where(e => e.TermId == termId)
                .OrderBy(e => e.DisplayOrder).ThenBy(e => e.StartDate)
                .ToListAsync(ct);
        }

        public Task<ScheduleEvent?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.ScheduleEvents.FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public async Task<ScheduleEvent> CreateAsync(ScheduleEvent ev, string actor, CancellationToken ct = default)
        {
            ev.Id = Guid.NewGuid();
            ev.CreatedAt = DateTimeOffset.UtcNow;
            ev.CreatedBy = actor;
            _context.ScheduleEvents.Add(ev);
            await _context.SaveChangesAsync(ct);
            return ev;
        }

        public async Task<bool> UpdateAsync(ScheduleEvent ev, string actor, CancellationToken ct = default)
        {
            var existing = await _context.ScheduleEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == ev.Id, ct);
            if (existing == null) return false;

            ev.CreatedAt = existing.CreatedAt;
            ev.CreatedBy = existing.CreatedBy;
            ev.UpdatedAt = DateTimeOffset.UtcNow;
            ev.UpdatedBy = actor;
            _context.ScheduleEvents.Update(ev);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var ev = await _context.ScheduleEvents.FindAsync(new object[] { id }, ct);
            if (ev == null) return DeleteOutcome.NotFound;

            try
            {
                _context.ScheduleEvents.Remove(ev);
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
