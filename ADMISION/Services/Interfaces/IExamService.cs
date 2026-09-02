using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Services.Interfaces
{
    public interface IExamService
    {
        Task<List<Modality>> GetActiveExamsAsync();
    }
}
