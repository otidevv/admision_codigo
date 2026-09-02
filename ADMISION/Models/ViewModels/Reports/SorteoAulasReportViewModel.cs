namespace ADMISION.Models.ViewModels.Reports
{
    public class SorteoAulasReportFilter
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
    }

    public class SorteoAulasReportViewModel
    {
        public SorteoAulasReportFilter Filter { get; set; } = new();
        public string? TermName { get; set; }
        public string? ModalityName { get; set; }
        public bool HasData { get; set; }
        public SorteoAulasSummary Summary { get; set; } = new();
        public List<SorteoAulasDetailItem> Details { get; set; } = new();
    }

    public class SorteoAulasSummary
    {
        public int TotalAsignados { get; set; }
        public int TotalAulas { get; set; }
        public int TotalAforo { get; set; }
        public List<SorteoAulasPavilionGroup> PorPabellon { get; set; } = new();
    }

    public class SorteoAulasPavilionGroup
    {
        public string PavilionCode { get; set; } = string.Empty;
        public string PavilionName { get; set; } = string.Empty;
        public List<SorteoAulasClassroomGroup> Groups { get; set; } = new();
        public int TotalAsignados => Groups.Sum(g => g.TotalAsignados);
        public int TotalAforo => Groups.Sum(g => g.Capacidad);
    }

    public class SorteoAulasClassroomGroup
    {
        public string GroupName { get; set; } = string.Empty;
        public List<SorteoAulasClassroomItem> Classrooms { get; set; } = new();
        public int TotalAsignados => Classrooms.Sum(c => c.Asignados);
        public int Capacidad => Classrooms.Sum(c => c.Capacidad);
    }

    public class SorteoAulasClassroomItem
    {
        public string ClassroomName { get; set; } = string.Empty;
        public int Capacidad { get; set; }
        public int Asignados { get; set; }
        public string? Docente { get; set; }
        public string? AreaTematica { get; set; }
        public int Piso { get; set; }
    }

    public class SorteoAulasDetailItem
    {
        public int Silla { get; set; }
        public string CodigoPostulante { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public string Aula { get; set; } = string.Empty;
        public string? Pabellon { get; set; }
        public string? FotoBase64 { get; set; }
        public byte[]? PhotoBytes { get; set; }
    }
}
