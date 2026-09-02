namespace ADMISION.Models.ViewModels.Reports
{
    public class SiriesReportViewModel
    {
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public int TotalPostulantes { get; set; }
        public int TotalIngresantes { get; set; }
        public List<SiriesReportItem> Items { get; set; } = new();
    }

    public class SiriesReportItem
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public string FechaNacimiento { get; set; } = string.Empty;
        public string Discapacidad { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public string Local { get; set; } = "CAMPUS UNIVERSITARIO";
        public string CarreraPrimeraOpcion { get; set; } = string.Empty;
        public string CarreraSegundaOpcion { get; set; } = string.Empty;
        public string ModalidadAdmision { get; set; } = string.Empty;
        public string ModalidadEstudios { get; set; } = "presencial";
        public string Puntaje { get; set; } = string.Empty;
        public string EsIngresante { get; set; } = "NO";
        public string CarreraIngreso { get; set; } = string.Empty;
        public string IdentidadEtnica { get; set; } = "mestizo";
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string CorreoPersonal { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
    }
}
