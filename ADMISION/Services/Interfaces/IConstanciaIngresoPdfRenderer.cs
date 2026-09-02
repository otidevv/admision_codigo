namespace ADMISION.Services.Interfaces
{
    public class ConstanciaIngresoModel
    {
        // Identidad del ingresante
        public string FullName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = "DNI";
        public string DocumentNumber { get; set; } = string.Empty;
        public string PostulantCode { get; set; } = string.Empty;

        // Datos académicos
        public string CareerName { get; set; } = string.Empty;
        public string ModalityName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;

        public System.DateTimeOffset IssuedAt { get; set; } = System.DateTimeOffset.Now;

        // Header institucional
        public string InstitutionName { get; set; } = "Universidad Nacional Amazónica de Madre de Dios";
        public string? Dependency { get; set; }

        // Recursos binarios
        public byte[]? LogoBytes { get; set; }
        public byte[]? SecondaryLogoBytes { get; set; }

        // Firmantes
        public string? DirectorCommissionName { get; set; }

        // Footer
        public string? FooterAddress { get; set; }

        // Marca de agua (opcional)
        public string? WatermarkText { get; set; }
    }

    public interface IConstanciaIngresoPdfRenderer
    {
        byte[] Render(ConstanciaIngresoModel model);
    }
}
