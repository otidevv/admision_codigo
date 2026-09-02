using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Services.Interfaces;

public interface IConsolidadoConfigService
{
    Task<List<Term>> GetTermsAsync();
    Task<List<PostulantTypeConfig>> GetConfigurationsAsync(Guid termId);
    Task<List<Career>> GetCareersAsync();
    Task<List<Modality>> GetModalitiesAsync(Guid termId);
    Task<List<TypeModality>> GetTypeModalitiesAsync(Guid termId);
    Task<bool> ExistsConfigurationAsync(Guid termId, int index);
    Task CreateConfigurationAsync(Guid termId, int index, string description, Guid? careerId, Guid? modalityId, Guid? typeModalityId, string createdBy);
    Task<bool> DeleteConfigurationAsync(Guid id);
}
