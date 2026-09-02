using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.ViewModels.Admin;

namespace ADMISION.Services.Interfaces
{
    public interface ITermService
    {
        Task<IReadOnlyList<Term>> GetAllAsync(CancellationToken ct = default);
        Task<Term?> GetByIdAsync(Guid id, CancellationToken ct = default);
        /// <summary>
        /// Devuelve el periodo marcado como activo con sus modalidades cargadas
        /// (ordenadas por ExamDate). Si no hay activo retorna el más reciente.
        /// Usado por la vista de Periodos para pintar la línea de tiempo de exámenes.
        /// </summary>
        Task<Term?> GetActiveWithModalitiesAsync(CancellationToken ct = default);
        Task<Term> CreateAsync(Term term, TermReplicationOptions options, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Term term, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Calcula el checklist de configuraciones críticas para que un postulante
        /// pueda inscribirse en el periodo dado: modalidades, vacantes, áreas
        /// temáticas, códigos de pago, requisitos, fechas de examen, etc.
        /// </summary>
        Task<TermConfigChecklistDto?> GetConfigChecklistAsync(Guid termId, CancellationToken ct = default);
    }

    /// <summary>
    /// Selección granular de qué tablas replicar al crear un nuevo término a
    /// partir del más reciente. <see cref="Enabled"/> es el master toggle:
    /// si está en <c>false</c> no se replica nada (las demás flags se ignoran).
    /// El resto inicia en <c>true</c> para preservar el comportamiento "replica
    /// todo" que tenía el flag binario anterior.
    /// </summary>
    public class TermReplicationOptions
    {
        public bool Enabled { get; set; }

        public bool PaymentCodes { get; set; } = true;
        public bool TematicAreaCareers { get; set; } = true;
        public bool Modalities { get; set; } = true;             // Cascada: TypeModalities + Vacancies + ModalityRequisites
        public bool PaymentCodeModalities { get; set; } = true;  // Requiere PaymentCodes + Modalities
        public bool ScheduleEvents { get; set; } = true;
        public bool PublicInfos { get; set; } = true;            // Requiere Modalities (si la info está vinculada a una modalidad)
        public bool Beneficiaries { get; set; } = true;

        public static TermReplicationOptions None() => new() { Enabled = false };
        public static TermReplicationOptions All() => new() { Enabled = true };
    }
}
