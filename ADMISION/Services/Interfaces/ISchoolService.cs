using ADMISION.ENTITIES.Models.Schools;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface ISchoolService
    {
        Task<PagedResult<SchoolListItem>> ListAsync(SchoolListQuery query, CancellationToken ct = default);
        Task<Schools> CreateAsync(Schools school, string actor, CancellationToken ct = default);
        Task<SchoolImportResult> ImportFromExcelAsync(Stream excelStream, string actor, CancellationToken ct = default);
    }

    public class SchoolListQuery : ListQuery
    {
        public Guid? DepartmentId { get; set; }
        public Guid? ProvinceId { get; set; }
        public Guid? DistrictId { get; set; }
        public string? Name { get; set; }
    }

    public record SchoolListItem(
        Guid Id,
        string Name,
        string Code,
        string? UgelName,
        string? Modality,
        string? Level,
        string? Management,
        string? Address,
        string? DistrictName,
        string? ProvinceName,
        string? DepartmentName);

    public record SchoolImportRow(
        string Region,
        string Province,
        string District,
        string Ugel,
        string Code,
        string Name,
        string Modality,
        string Level,
        string Management);

    public record SchoolImportError(SchoolImportRow Row, string Error);

    public class SchoolImportResult
    {
        public int ImportedCount { get; init; }
        public IReadOnlyList<SchoolImportError> Errors { get; init; } = Array.Empty<SchoolImportError>();
    }
}
