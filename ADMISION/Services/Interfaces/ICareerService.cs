using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    public interface ICareerService
    {
        Task<PagedResult<CareerListItem>> ListAsync(CareerListQuery query, CancellationToken ct = default);
        Task<Career?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Career> CreateAsync(Career career, CareerFiles files, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Career career, CareerFiles files, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);

        Task<int> AddImagesAsync(Guid careerId, IEnumerable<IFormFile> files, string actor, CancellationToken ct = default);
        Task<bool> DeleteImageAsync(Guid careerId, Guid imageId, CancellationToken ct = default);
    }

    public class CareerListQuery : ListQuery
    {
        public Guid? FacultyId { get; set; }
    }

    public record CareerListItem(
        Guid Id,
        string Name,
        string Code,
        string? ProgramNumber,
        bool IsActive,
        string? FacultyName);

    public record CareerFiles(
        IFormFile? Logo,
        IFormFile? Banner,
        IFormFile? StudyPlan,
        IReadOnlyList<IFormFile>? GalleryImages = null);
}
