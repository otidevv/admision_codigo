namespace ADMISION.Models.ViewModels.Reports
{
    public class IngresantesReportViewModel
    {
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public Guid? ModalityId { get; set; }
        public string? ModalityName { get; set; }
        public Guid? TypeModalityId { get; set; }
        public string? TypeModalityName { get; set; }
        public Guid? TypePostulantId { get; set; }
        public string? TypePostulantName { get; set; }
        public Guid? CareerId { get; set; }
        public string? CareerName { get; set; }
        public Guid? TematicAreaId { get; set; }
        public string? TematicAreaName { get; set; }
        public string? SegundaCarrera { get; set; }
        public string? TipoReporte { get; set; } = "consolidado";
        public int TotalIngresantes { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalIngresantes / PageSize) : 0;
        public List<IngresantesReportItem> Items { get; set; } = new();
    }

    public class IngresantesReportItem
    {
        public string CodigoEstudiante { get; set; } = string.Empty;
        public string Examen { get; set; } = string.Empty;
        public string TipoModalidad { get; set; } = string.Empty;
        public string TipoPostulante { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string CarreraProfesional { get; set; } = string.Empty;
        public string Tema { get; set; } = string.Empty;
        public decimal? Nota { get; set; }
        public bool IsAdmission { get; set; }
        public string SegundaCarrera { get; set; } = "0";
        public string SegundaCarreraText => SegundaCarrera == "1" ? "SÍ" : "NO";
        public string Estado => IsAdmission ? "INGRESO" : "NO INGRESO";
    }
}
