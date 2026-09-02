using ADMISION.ENTITIES.Models.Info;

namespace ADMISION.Services.Interfaces
{
    public interface IFaqService
    {
        Task<IReadOnlyList<FaqItem>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
        Task<IReadOnlyList<FaqItem>> GetPublicAsync(CancellationToken ct = default);
        Task<FaqItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<FaqItem> CreateAsync(FaqItem item, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(FaqItem item, string actor, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);

        /// <summary>
        /// Busca la mejor coincidencia para la pregunta del usuario y devuelve
        /// la respuesta junto con las top-N sugerencias similares.
        /// </summary>
        Task<FaqAskResult> AskAsync(string query, int suggestionsTop = 3, CancellationToken ct = default);

        /// <summary>Opciones raíz (sin padre) para el menú principal del chatbot.</summary>
        Task<IReadOnlyList<FaqItem>> GetRootOptionsAsync(CancellationToken ct = default);

        /// <summary>Obtiene una opción con sus hijos activos para navegación jerárquica.</summary>
        Task<FaqItem?> GetOptionWithChildrenAsync(Guid id, CancellationToken ct = default);
    }

    /// <summary>Resultado del intento de match contra una pregunta del usuario.</summary>
    public class FaqAskResult
    {
        public bool Matched { get; set; }
        /// <summary>Score del best match (0..1). 1 = exacto.</summary>
        public double Score { get; set; }
        public FaqItem? Best { get; set; }
        public List<FaqSuggestion> Suggestions { get; set; } = new();
    }

    public class FaqSuggestion
    {
        public Guid Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? Category { get; set; }
        public double Score { get; set; }
    }
}
