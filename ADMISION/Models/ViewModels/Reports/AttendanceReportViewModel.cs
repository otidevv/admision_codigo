namespace ADMISION.Models.ViewModels.Reports
{
    public class AttendanceReportFilter
    {
        public Guid TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public string AttendanceStatus { get; set; } = "all"; // "all" | "attended" | "not_attended"
    }

    public class AttendanceReportViewModel
    {
        public AttendanceReportFilter Filter { get; set; } = new();
        public string? TermName { get; set; }
        public string? ModalityName { get; set; }
        public bool HasData { get; set; }
        public int TotalAssigned { get; set; }
        public int TotalAttended { get; set; }
        public int TotalMissing => TotalAssigned - TotalAttended;
        public List<AttendanceReportItem> Items { get; set; } = new();
        public List<AttendanceClassroomSummary> SummaryByClassroom { get; set; } = new();
        public List<AttendanceAreaSummary> SummaryByArea { get; set; } = new();
        public List<AttendanceCareerSummary> SummaryByCareer { get; set; } = new();
    }

    public class AttendanceReportItem
    {
        public int SeatNumber { get; set; }
        public string CodePostulant { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Modality { get; set; } = string.Empty;
        public string TypeModality { get; set; } = string.Empty;
        public string Disability { get; set; } = string.Empty;
        public string Classroom { get; set; } = string.Empty;
        public string Docente { get; set; } = string.Empty;
        public string AttendanceStatus { get; set; } = string.Empty; // "Asistió" | "No asistió"
        public string TematicArea { get; set; } = string.Empty;
        public string Career { get; set; } = string.Empty;
    }

    public class AttendanceClassroomSummary
    {
        public string Classroom { get; set; } = string.Empty;
        public int Attended { get; set; }
        public int Missing { get; set; }
        public int Total => Attended + Missing;
    }

    public class AttendanceAreaSummary
    {
        public string Area { get; set; } = string.Empty;
        public int Attended { get; set; }
        public int Missing { get; set; }
        public int Total => Attended + Missing;
    }

    public class AttendanceCareerSummary
    {
        public string Career { get; set; } = string.Empty;
        public int Attended { get; set; }
        public int Missing { get; set; }
        public int Total => Attended + Missing;
    }
}
