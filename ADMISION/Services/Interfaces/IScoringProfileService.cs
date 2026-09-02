using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface IScoringProfileService
    {
        Task<PagedResult<ScoringProfileListItem>> ListAsync(ScoringProfileListQuery query, CancellationToken ct = default);
        Task<ScoringProfileDetail?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<SaveResult> CreateAsync(ScoringProfile profile, IReadOnlyList<ScoringProfileRange> ranges, string actor, CancellationToken ct = default);
        Task<SaveResult> UpdateAsync(ScoringProfile profile, IReadOnlyList<ScoringProfileRange> ranges, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public class ScoringProfileListQuery : ListQuery
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public bool? IsWeighted { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ScoringProfileListItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsWeighted { get; init; }
        public decimal PuntosCorrecta { get; init; }
        public decimal PuntosBlanco { get; init; }
        public decimal PuntosIncorrecta { get; init; }
        public bool IsActive { get; init; }
        public int RangeCount { get; init; }
        public string? TermName { get; init; }
        public string? ModalityName { get; init; }
    }

    public class ScoringProfileRangeDetail
    {
        public Guid Id { get; init; }
        public int FromQuestion { get; init; }
        public int ToQuestion { get; init; }
        public decimal PuntosCorrecta { get; init; }
        public int DisplayOrder { get; init; }
    }

    public class ScoringProfileDetail
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsWeighted { get; init; }
        public decimal PuntosCorrecta { get; init; }
        public decimal PuntosBlanco { get; init; }
        public decimal PuntosIncorrecta { get; init; }
        public decimal NotaMinimaIngreso { get; init; }
        public bool AplicarVigesimal { get; init; }
        public string ManejoAnuladas { get; init; } = "Ignorar";
        public Guid? TermId { get; init; }
        public Guid? ModalityId { get; init; }
        public Guid? TypeModalityId { get; init; }
        public Guid? CareerId { get; init; }
        public bool IsActive { get; init; }
        public string? TermName { get; init; }
        public string? ModalityName { get; init; }
        public string? TypeModalityName { get; init; }
        public string? CareerName { get; init; }
        public IReadOnlyList<ScoringProfileRangeDetail> Ranges { get; init; } = Array.Empty<ScoringProfileRangeDetail>();
    }
}
