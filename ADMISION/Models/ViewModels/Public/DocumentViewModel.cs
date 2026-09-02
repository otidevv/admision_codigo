namespace ADMISION.Models.ViewModels.Public
{
    public class DocumentViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // MIME o extensión
        public string FileSize { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty; // "Prospecto" | "Archivo"
        public string Badge { get; set; } = string.Empty; // Texto secundario (p.ej. nombre del proceso)
    }
}
