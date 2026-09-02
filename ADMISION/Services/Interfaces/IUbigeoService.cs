using ADMISION.ENTITIES.Models.Ubigeo;

namespace ADMISION.Services.Interfaces
{
    public interface IUbigeoService
    {
        Task<IReadOnlyList<UbigeoOption>> GetCountriesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<UbigeoOption>> GetDepartmentsAsync(Guid countryId, CancellationToken ct = default);
        Task<IReadOnlyList<UbigeoOption>> GetAllDepartmentsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<UbigeoOption>> GetProvincesAsync(Guid departmentId, CancellationToken ct = default);
        Task<IReadOnlyList<UbigeoOption>> GetDistrictsAsync(Guid provinceId, CancellationToken ct = default);

        Task<UbigeoLookupResult?> FindByCodeAsync(string code, CancellationToken ct = default);

        Task<UbigeoCounts> GetCountsAsync(CancellationToken ct = default);

        Task<UbigeoImportResult> ImportCsvAsync(Stream csvStream, Guid countryId, string actor, CancellationToken ct = default);

        // Gestión manual CRUD
        Task<List<DepartmentWithProvincesDto>> GetFullUbigeoDataAsync(Guid countryId, CancellationToken ct = default);

        Task<Department> CreateDepartmentAsync(string name, string code, Guid countryId, string actor, CancellationToken ct = default);
        Task<Department> UpdateDepartmentAsync(Guid id, string name, string code, string? actor, CancellationToken ct = default);
        Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default);

        Task<Provincie> CreateProvinceAsync(string name, string code, Guid departmentId, string actor, CancellationToken ct = default);
        Task<Provincie> UpdateProvinceAsync(Guid id, string name, string code, string? actor, CancellationToken ct = default);
        Task DeleteProvinceAsync(Guid id, CancellationToken ct = default);

        Task<Distrit> CreateDistrictAsync(string name, string code, Guid provinceId, string actor, CancellationToken ct = default);
        Task<Distrit> UpdateDistrictAsync(Guid id, string name, string code, string? actor, CancellationToken ct = default);
        Task DeleteDistrictAsync(Guid id, CancellationToken ct = default);
    }

    public record UbigeoOption(Guid Id, string Name);
    public record UbigeoCounts(int Departments, int Provinces, int Districts);
    public record UbigeoImportResult(int NewDepartments, int NewProvinces, int NewDistricts);
    public record UbigeoLookupResult(
        Guid DistritId, string DistritName,
        Guid ProvinceId, string ProvinceName,
        Guid DepartmentId, string DepartmentName);

    // DTOs para el árbol completo de ubigeo
    public record DepartmentWithProvincesDto(Guid Id, string Name, string Code, List<ProvinceWithDistrictsDto> Provinces);
    public record ProvinceWithDistrictsDto(Guid Id, string Name, string Code, List<DistrictSimpleDto> Districts);
    public record DistrictSimpleDto(Guid Id, string Name, string Code);
}
