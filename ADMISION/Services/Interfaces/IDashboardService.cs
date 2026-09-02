using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.ViewModels.Admin;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Agregaciones del dashboard administrativo: KPIs (total/edad/género),
    /// distribuciones (áreas, carreras, regiones), choropleth Perú/mundo,
    /// traslados y transferencias bancarias recientes.
    /// </summary>
    public interface IDashboardService
    {
        Task<IReadOnlyList<Term>> GetTermsAsync(CancellationToken ct = default);

        /// <summary>
        /// Construye el dashboard del término indicado. Acepta filtros opcionales
        /// (modalidad, tipo de modalidad, carrera y área temática); cuando se
        /// proporcionan, las inscripciones se acotan acordemente antes de calcular
        /// KPIs, gráficos y mapas.
        /// </summary>
        Task<AdminDashboardDto> BuildDashboardAsync(
            Guid termId,
            Guid? modalityId = null,
            Guid? typeModalityId = null,
            Guid? careerId = null,
            Guid? tematicAreaId = null,
            CancellationToken ct = default);
    }
}
