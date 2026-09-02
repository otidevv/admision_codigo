// DataTableModels.cs
namespace ADMISION.Models.Shared;

/// <summary>
/// Define una columna del DataTable genérico.
/// </summary>
public class DataTableColumn
{
    /// <summary>Texto del encabezado.</summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Nombre del campo en el objeto JSON.
    /// Soporta notación de punto para objetos anidados: "distrit.province.name"
    /// </summary>
    public string Field { get; set; } = "";

    /// <summary>
    /// Tipo de renderizado:
    ///   text | combined | badge | image | boolean | date | currency | code | progress | custom
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>Campo secundario para tipo "combined" (subtítulo).</summary>
    public string? SubField { get; set; }

    /// <summary>Formato de fecha para tipo "date". Ej: "dd/MM/yyyy", "dd/MM/yyyy HH:mm".</summary>
    public string? DateFormat { get; set; }

    /// <summary>Símbolo monetario para tipo "currency". Default: "S/".</summary>
    public string? CurrencySymbol { get; set; }

    /// <summary>Si true, la imagen se muestra redonda (avatar).</summary>
    public bool ImageRound { get; set; }

    /// <summary>Tamaño en px de la imagen. Default: 32.</summary>
    public int ImageSize { get; set; } = 32;

    /// <summary>
    /// Mapa de colores para tipo "badge".
    /// Key = valor del campo, Value = color (green|red|yellow|blue|purple|gray|orange).
    /// Ej: new() { {"Activo","green"}, {"Inactivo","gray"} }
    /// </summary>
    public Dictionary<string, string>? BadgeMap { get; set; }

    /// <summary>Nombre de la función JS registrada con DT.registerRenderer() para tipo "custom".</summary>
    public string? RenderFn { get; set; }

    /// <summary>Alineación: "left" | "center" | "right". Default: "left".</summary>
    public string Align { get; set; } = "left";

    /// <summary>Ancho CSS de la columna. Ej: "8rem", "120px", "15%".</summary>
    public string? Width { get; set; }

    /// <summary>Si true, la columna tiene ordenamiento al hacer clic en el header.</summary>
    public bool Sortable { get; set; }

    /// <summary>Si true, la columna se oculta en pantallas pequeñas.</summary>
    public bool Hidden { get; set; }
}

/// <summary>
/// Define una acción del menú popover de cada fila.
/// </summary>
public class DataTableAction
{
    /// <summary>Clave del evento emitido: dt:action → detail.key === este valor.</summary>
    public string Key { get; set; } = "";

    /// <summary>Texto visible en el menú.</summary>
    public string Label { get; set; } = "";

    /// <summary>Clase FontAwesome del ícono. Ej: "fa-edit", "fa-trash-alt".</summary>
    public string Icon { get; set; } = "fa-circle";

    /// <summary>Si true, el ítem se muestra en rojo (acciones destructivas).</summary>
    public bool Danger { get; set; }

    /// <summary>Si se define, el clic navega a esta URL en lugar de emitir evento.</summary>
    public string? Href { get; set; }

    /// <summary>Atributo target para el enlace (ej: "_blank"). Solo aplica si Href no es nulo.</summary>
    public string? Target { get; set; }

    /// <summary>Si true, agrega separador visual antes de este ítem.</summary>
    public bool Separator { get; set; }
}

/// <summary>
/// Respuesta estándar de endpoints AJAX para el DataTable.
/// Serializa directamente como JSON.
/// </summary>
public class DataTableResponse<T>
{
    public List<T> Data       { get; set; } = new();
    public int TotalItems     { get; set; }
    public int TotalPages     { get; set; }
    public int PageSize       { get; set; }
    public int CurrentPage    { get; set; }

    public static DataTableResponse<T> From(IQueryable<T> query, int page, int pageSize)
    {
        var total = query.Count();
        var data  = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new()
        {
            Data        = data,
            TotalItems  = total,
            TotalPages  = (int)Math.Ceiling((double)total / pageSize),
            PageSize    = pageSize,
            CurrentPage = page,
        };
    }
}