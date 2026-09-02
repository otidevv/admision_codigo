using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Services.Interfaces
{
    public interface IScheduleEventService
    {
        Task<IReadOnlyList<ScheduleEvent>> GetByTermAsync(Guid termId, CancellationToken ct = default);
        Task<ScheduleEvent?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ScheduleEvent> CreateAsync(ScheduleEvent ev, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(ScheduleEvent ev, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
