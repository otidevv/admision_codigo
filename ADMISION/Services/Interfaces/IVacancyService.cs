using ADMISION.Models.ViewModels.Admin;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Gestión de vacantes por modalidad/tipo: la "matriz" de cupos por carrera.
    /// </summary>
    public interface IVacancyService
    {
        Task<VacanciesMatrixViewModel?> BuildMatrixAsync(Guid modalityId, CancellationToken ct = default);
        Task SaveMatrixAsync(Guid modalityId, Dictionary<string, int> quantities, string actor, CancellationToken ct = default);
    }
}
