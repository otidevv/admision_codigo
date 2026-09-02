using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Models.ViewModels.Public
{
    public class ScheduleViewModel
    {
        public IReadOnlyList<Term> Terms { get; set; } = Array.Empty<Term>();
        public Term? SelectedTerm { get; set; }
        public IReadOnlyList<SchedulePhaseGroup> Phases { get; set; } = Array.Empty<SchedulePhaseGroup>();
    }

    public class SchedulePhaseGroup
    {
        public string PhaseKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool Accent { get; set; }
        public string Location { get; set; } = string.Empty;
        public IReadOnlyList<ScheduleEvent> Items { get; set; } = Array.Empty<ScheduleEvent>();
    }

    public class ModalityPublicViewModel
    {
        public IReadOnlyList<Term> Terms { get; set; } = Array.Empty<Term>();
        public Term? SelectedTerm { get; set; }
        public IReadOnlyList<ModalityCard> Modalities { get; set; } = Array.Empty<ModalityCard>();
    }

    public class ModalityCard
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string ModalityName { get; set; } = string.Empty;
        public string Title => string.IsNullOrWhiteSpace(TypeName)
            ? ModalityName
            : $"{TypeName} ({ModalityName})";
        public string Summary { get; set; } = string.Empty;
        public string IconKey { get; set; } = "ti ti-file-text";
        public string? Badge { get; set; }
        public bool IsActive { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateOnly? ExamDate { get; set; }
        public DateOnly? ResultsPublicationDate { get; set; }
        public IReadOnlyList<string> PostulationRequirements { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> EntryRequirements { get; set; } = Array.Empty<string>();
        public IReadOnlyList<PaymentRequirement> PaymentRequirements { get; set; } = Array.Empty<PaymentRequirement>();
        public IReadOnlyList<PublicInfoBlock> InfoBlocks { get; set; } = Array.Empty<PublicInfoBlock>();
    }

    public class PaymentRequirement
    {
        public string Code { get; set; } = string.Empty;
        public string Concept { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class PublicInfoBlock
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    public class CareersPublicViewModel
    {
        public IReadOnlyList<FacultyCareers> Faculties { get; set; } = Array.Empty<FacultyCareers>();
    }

    public class FacultyCareers
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IReadOnlyList<Career> Careers { get; set; } = Array.Empty<Career>();
    }

    public class VacanciesPublicViewModel
    {
        public string ResolVacancies {  get; set; } = string.Empty;
        public IReadOnlyList<Term> Terms { get; set; } = Array.Empty<Term>();
        public Term? SelectedTerm { get; set; }
        public IReadOnlyList<ModalityColumnGroup> ModalityGroups { get; set; } = Array.Empty<ModalityColumnGroup>();
        public IReadOnlyList<VacancyColumn> Columns { get; set; } = Array.Empty<VacancyColumn>();
        public IReadOnlyList<FacultyVacancies> Faculties { get; set; } = Array.Empty<FacultyVacancies>();
        public int TotalVacancies { get; set; }
    }

    public class ModalityColumnGroup
    {
        public Guid ModalityId { get; set; }
        public string ModalityName { get; set; } = string.Empty;
        public int ColumnCount { get; set; }
        public bool HasSubHeaders { get; set; }
    }

    public class VacancyColumn
    {
        public Guid ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public string Header { get; set; } = string.Empty;
    }

    public class FacultyVacancies
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "ti ti-book";
        public IReadOnlyList<CareerVacancyRow> Careers { get; set; } = Array.Empty<CareerVacancyRow>();
    }

    public class CareerVacancyRow
    {
        public string CareerName { get; set; } = string.Empty;
        public IReadOnlyList<int> Values { get; set; } = Array.Empty<int>();
        public int Total { get; set; }
    }
}
