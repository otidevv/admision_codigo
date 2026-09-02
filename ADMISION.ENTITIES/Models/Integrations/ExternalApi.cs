using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Integrations
{
    [Table("ExternalApi", Schema = "Integrations")]
    public class ExternalApi
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // GET | POST | PUT | DELETE
        public string HttpMethod { get; set; } = "GET";

        // URL completa. Soporta placeholders {nombreParametro} reemplazados en runtime.
        public string Url { get; set; } = string.Empty;

        // None | Bearer | ApiKey | Basic
        public string AuthType { get; set; } = "None";
        // Para AuthType=ApiKey, header donde colocar la key (ej. "X-Api-Key", "Authorization").
        public string? AuthHeaderName { get; set; }
        // Token / api key / "user:password" (Basic). Tratado como sensible: se redacta al
        // serializar para auditoría (ver AuditInterceptor) y nunca se devuelve al cliente.
        public string? AuthValue { get; set; }

        // JSON con la definición de parámetros que debe pedir el formulario:
        // [{ "key":"document", "label":"DNI", "required":true, "in":"url|body|query" }]
        public string? RequestParametersJson { get; set; }

        // JSON con headers extra: { "Accept": "application/json", "Content-Type": "application/json" }
        public string? HeadersJson { get; set; }

        // Plantilla del body (para POST/PUT). Soporta placeholders {nombreParametro}.
        // Si no se define, en POST se envía un objeto JSON con todos los parámetros como propiedades.
        public string? RequestBodyTemplate { get; set; }

        // JSON con la definición de campos a mostrar en la tabla del resultado:
        // [{ "path":"data.nombres", "label":"Nombres" }, { "path":"data.apellidoPaterno", "label":"Apellido Paterno" }]
        // Si no se define, se hace flatten automático del primer nivel del JSON.
        public string? ResponseFieldsJson { get; set; }

        // Generic | Academic | Payment
        public string Category { get; set; } = "Generic";

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;
    }
}
