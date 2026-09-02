namespace ADMISION.Models.ViewModels.Reports
{
    public class TematicAreaReportViewModel
    {
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public Guid? ModalityId { get; set; }
        public string? ModalityName { get; set; }
        public Guid? TypeModalityId { get; set; }
        public string? TypeModalityName { get; set; }
        public Guid? TypePostulantId { get; set; }
        public string? TypePostulantName { get; set; }

        public int TotalInscripciones { get; set; }
        public List<TematicAreaReportItem> Areas { get; set; } = new();
    }

    public class TematicAreaReportItem
    {
        public Guid? TematicAreaId { get; set; }
        public string AreaCode { get; set; } = string.Empty;
        public int Subtotal { get; set; }
        public List<CareerReportItem> Careers { get; set; } = new();
    }

    public class CareerReportItem
    {
        public Guid CareerId { get; set; }
        public string CareerCode { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public int Inscritos { get; set; }
    }
}
