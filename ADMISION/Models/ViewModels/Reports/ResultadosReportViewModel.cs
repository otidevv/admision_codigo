namespace ADMISION.Models.ViewModels.Reports
{
    public class ResultadosReportViewModel
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
        public string? Condicion { get; set; }
        public int TotalRecords { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
        public List<ResultadosReportItem> Items { get; set; } = new();
    }

    public class ResultadosReportItem
    {
        public int Nro { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string ApellidosNombres { get; set; } = string.Empty;
        public string Examen { get; set; } = string.Empty;
        public string TipoModalidad { get; set; } = string.Empty;
        public string TipoPostulante { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
        public string Correctas { get; set; } = string.Empty;
        public string Blancas { get; set; } = string.Empty;
        public string Puntaje { get; set; } = string.Empty;
        public string Nota { get; set; } = string.Empty;
        public string Condicion { get; set; } = string.Empty;
    }
}
