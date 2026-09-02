using System.Collections.Generic;
using System.Threading.Tasks;

namespace ADMISION.Services.Interfaces
{
    public class DocumentResult
    {
        public byte[] PdfBytes { get; set; } = System.Array.Empty<byte>();
        public string FileName { get; set; } = "documento.pdf";
    }

    public class DocumentOptions
    {
        public string PageSize { get; set; } = "A4";
        public bool Landscape { get; set; }
        public string? Margin { get; set; }
        public string? WatermarkText { get; set; }
        public System.Guid? PostulantId { get; set; }
        public System.Guid? InscriptionId { get; set; }
        public string? FileName { get; set; }
    }

    public interface IDocumentService
    {
        Task<DocumentResult> GenerateConstanciaIngresoPdfAsync(
            ConstanciaIngresoModel model,
            DocumentOptions? options = null,
            string? userName = null);

        Task<DocumentResult> GeneratePdfFromTemplateAsync(
            string templateName,
            IDictionary<string, object?> data,
            DocumentOptions? options = null);

        Task<string> RenderHtmlAsync(
            string templateName,
            IDictionary<string, object?> data,
            DocumentOptions? options = null);
    }
}
