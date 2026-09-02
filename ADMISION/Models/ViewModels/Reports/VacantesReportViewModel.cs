namespace ADMISION.Models.ViewModels.Reports
{
    public class VacantesReportFilter
    {
        public Guid? TermId { get; set; }
        public string ReportType { get; set; } = "vacantes";
    }

    public class VacantesReportViewModel
    {
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public string ReportType { get; set; } = "vacantes";

        public IReadOnlyList<VacantesModalityGroup> ModalityGroups { get; set; } = Array.Empty<VacantesModalityGroup>();
        public IReadOnlyList<VacantesColumnInfo> Columns { get; set; } = Array.Empty<VacantesColumnInfo>();
        public IReadOnlyList<VacantesFacultyGroup> Faculties { get; set; } = Array.Empty<VacantesFacultyGroup>();
        public IReadOnlyList<int> ColumnTotals { get; set; } = Array.Empty<int>();
        public int GrandTotal { get; set; }
    }

    public class VacantesModalityGroup
    {
        public Guid ModalityId { get; set; }
        public string ModalityName { get; set; } = string.Empty;
        public int ColumnCount { get; set; }
        public bool HasSubHeaders { get; set; }
    }

    public class VacantesColumnInfo
    {
        public Guid ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public string Header { get; set; } = string.Empty;
    }

    public class VacantesFacultyGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "ti ti-book";
        public IReadOnlyList<VacantesCareerRow> Careers { get; set; } = Array.Empty<VacantesCareerRow>();
        public int Subtotal { get; set; }
    }

    public class VacantesCareerRow
    {
        public string CareerName { get; set; } = string.Empty;
        public IReadOnlyList<int> Values { get; set; } = Array.Empty<int>();
        public int Total { get; set; }
    }
}
