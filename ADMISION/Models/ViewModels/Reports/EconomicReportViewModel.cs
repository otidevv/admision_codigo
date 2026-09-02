namespace ADMISION.Models.ViewModels.Reports
{
    public class EconomicReportFilter
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? TypePostulantId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class EconomicReportViewModel
    {
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public Guid? ModalityId { get; set; }
        public string? ModalityName { get; set; }
        public Guid? TypeModalityId { get; set; }
        public string? TypeModalityName { get; set; }
        public Guid? TypePostulantId { get; set; }
        public string? TypePostulantName { get; set; }

        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;

        public decimal TotalMonto { get; set; }
        public int ConPago { get; set; }
        public int SinPago { get; set; }

        public List<EconomicReportItem> Items { get; set; } = new();
    }

    public class EconomicReportItem
    {
        public string Ciclo { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Examen { get; set; } = string.Empty;
        public string Modalidad { get; set; } = string.Empty;
        public string TipoPostulante { get; set; } = string.Empty;
        public string Descuento { get; set; } = string.Empty;
        public string Monto { get; set; } = string.Empty;
    }
}
