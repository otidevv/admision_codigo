namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Lookups read-only de tablas de dominio que se consumen como dropdowns
    /// (terms, faculties, modalities, careers, etc.). Centraliza las queries
    /// duplicadas entre controladores admin/public.
    /// </summary>
    public interface ICatalogService
    {
        Task<IReadOnlyList<CatalogOption>> GetTermsAsync(bool onlyActive = false, CancellationToken ct = default);
        Task<IReadOnlyList<CatalogOption>> GetFacultiesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<CatalogOption>> GetCareersAsync(Guid? facultyId = null, bool onlyActive = false, CancellationToken ct = default);
        Task<IReadOnlyList<CatalogOption>> GetModalitiesAsync(Guid? termId = null, bool onlyActive = false, CancellationToken ct = default);
        Task<IReadOnlyList<TypeModalityOption>> GetTypeModalitiesAsync(Guid modalityId, bool onlyActive = true, CancellationToken ct = default);
        Task<IReadOnlyList<TypeModalityOption>> GetAllTypeModalitiesAsync(bool onlyActive = true, CancellationToken ct = default);
        Task<IReadOnlyList<CatalogOption>> GetTypePostulantsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<CatalogOption>> GetTematicAreasByTermAsync(Guid termId, CancellationToken ct = default);
    }

    public record CatalogOption(Guid Id, string Name);
    public record TypeModalityOption(Guid Id, string Name, decimal DiscountPercentage, Guid ModalityId);
}
