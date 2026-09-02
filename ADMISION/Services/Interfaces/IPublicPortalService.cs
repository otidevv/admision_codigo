using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.ViewModels.Public;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Datos read-only consumidos por las páginas públicas (HomeController).
    /// Cada método devuelve el ViewModel ya armado para que el controller
    /// solo haga `return View(vm)`.
    /// </summary>
    public interface IPublicPortalService
    {
        Task<HomeViewModel> GetHomeAsync(CancellationToken ct = default);
        Task<DocumentsPageViewModel?> GetDocumentsPageAsync(string category, CancellationToken ct = default);
        Task<ResultsPublicViewModel> GetResultsAsync(Guid? termId, CancellationToken ct = default);
        Task<VacanciesPublicViewModel> GetVacanciesAsync(Guid? termId, CancellationToken ct = default);
        Task<CareersPublicViewModel> GetCareersAsync(CancellationToken ct = default);
        Task<CareerDetailResult?> GetCareerDetailAsync(Guid id, CancellationToken ct = default);
        Task<ScheduleViewModel> GetScheduleAsync(Guid? termId, CancellationToken ct = default);
        Task<ModalityPublicViewModel> GetModalityAsync(Guid? termId, CancellationToken ct = default);
    }

    public record CareerDetailResult(Career Career, Term? LatestTerm, int TotalVacancies);
}
