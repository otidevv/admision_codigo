using Microsoft.EntityFrameworkCore;

namespace ADMISION.Models.Shared;

/// <summary>
/// Resultado paginado consumible desde controladores y vistas.
/// Construirlo desde un IQueryable&lt;T&gt; con CreateAsync para evitar dos round-trips innecesarios.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> source, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 500) pageSize = 500;

        var total = await source.CountAsync(ct);
        var items = total == 0
            ? new List<T>()
            : await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Page = page,
            PageSize = pageSize
        };
    }
}

/// <summary>
/// Parámetros base para listados con búsqueda + orden + paginación.
/// Cada servicio puede heredar y añadir sus propios filtros tipados.
/// </summary>
public class ListQuery
{
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public bool IsDescending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Error de validación que un servicio devuelve al controller para que lo
/// agregue al ModelState (Field puede ser nombre de propiedad del modelo
/// o cadena vacía para errores generales).
/// </summary>
public record ValidationError(string Field, string Message);

/// <summary>
/// Resultado de operaciones de Create/Update. Cuando Succeeded == false,
/// Errors contiene los detalles tipados; NotFound discrimina "no existe"
/// de "tiene problemas de validación".
/// </summary>
public class SaveResult
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    public static SaveResult Ok() => new() { Succeeded = true };
    public static SaveResult Invalid(params ValidationError[] errors) => new() { Succeeded = false, Errors = errors };
    public static SaveResult Invalid(IEnumerable<ValidationError> errors) => new() { Succeeded = false, Errors = errors.ToArray() };
    public static SaveResult NotFoundResult() => new() { Succeeded = false, NotFound = true };
}
