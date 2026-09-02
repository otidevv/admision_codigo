using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Consultas y edición de inscripciones de postulantes (admin).
    /// </summary>
    public interface IPostulantQueryService
    {
        Task<PagedResult<PostulantInscriptionListItem>> ListAsync(PostulantInscriptionListQuery query, CancellationToken ct = default);
        Task<PostulantInscriptionEditData?> GetForEditAsync(Guid id, CancellationToken ct = default);
        Task<SaveResult> UpdateAsync(Guid id, Inscription model, string actor, CancellationToken ct = default);
    }

    public class PostulantInscriptionListQuery : ListQuery
    {
        public Guid? AreaId { get; set; }
        public Guid? TermId { get; set; }
        public Guid? CareerId { get; set; }
        public Guid? FacultyId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? TypePostulantId { get; set; }
        public string? State { get; set; }
    }

    public record PostulantInscriptionListItem(
        Guid Id,
        Guid PostulantId,
        string CodePostulant,
        DateTimeOffset CreatedAt,
        string State,
        string? FullName,
        string? Document,
        string? DocumentType,
        string? CareerName,
        string? CareerArea,
        string? ModalityName,
        string? TypeModalityName);

    /// <summary>
    /// Inscripción + datos derivados para la página de edición (área temática
    /// computada desde el término actual de la modalidad).
    /// </summary>
    public record PostulantInscriptionEditData(Inscription Inscription, string? TematicAreaCode);
}
