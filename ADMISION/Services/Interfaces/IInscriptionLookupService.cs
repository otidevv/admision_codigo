namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Lookups y datos auxiliares del formulario público de inscripción.
    /// Centraliza los endpoints AJAX (`/check-user`, `/type-modalities/{id}`, etc.)
    /// y la carga inicial de datos del GET `/inscription`.
    /// </summary>
    public interface IInscriptionLookupService
    {
        Task<InscriptionFormData> GetFormDataAsync(CancellationToken ct = default);
        Task<DateTime> GetExamEndDateAsync(Guid? modalityId, CancellationToken ct = default);

        Task<UserAutofillData?> CheckUserAsync(string docType, string docNumber, CancellationToken ct = default);

        Task<IReadOnlyList<TypeModalityWithKind>> GetTypeModalitiesAsync(Guid modalityId, CancellationToken ct = default);
        Task<ModalityDates?> GetModalityInfoAsync(Guid modalityId, CancellationToken ct = default);

        Task<IReadOnlyList<NamedOption>> GetUniversitiesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<NamedOption>> GetCareersListAsync(CancellationToken ct = default);
        Task<IReadOnlyList<SchoolOption>> GetSchoolsByDistrictAsync(Guid districtId, CancellationToken ct = default);

        Task<IReadOnlyList<RequirementOption>> GetRequirementsAsync(Guid modalityId, Guid? typeModalityId, Guid? typePostulantId, CancellationToken ct = default);
        Task<RequirementOption?> GetTypePostulantRequirementAsync(Guid typePostulantId, CancellationToken ct = default);
        Task<PaymentInfoResult> GetPaymentInfoAsync(Guid modalityId, Guid? typeModalityId, Guid? typePostulantId, CancellationToken ct = default);

        Task<IReadOnlyList<InscriptionSearchResult>> FindByDocumentAsync(string docType, string docNumber, CancellationToken ct = default);
        Task<Dictionary<Guid, List<Guid>>> GetModalityCareerMapAsync(CancellationToken ct = default);
        Task<Dictionary<Guid, List<Guid>>> GetTypeModalityCareerMapAsync(CancellationToken ct = default);
        Task<Dictionary<Guid, ModalityFlags>> GetModalityFlagsAsync(CancellationToken ct = default);
    }

    public record ModalityFlags(bool RequiresProfilePhoto, bool IsMockExam, bool RequiresSchoolType, bool RequiresEducationalLevel, bool RequiresGrade);

    public class FileValidationInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public bool IsValidated { get; set; }
        public string? Observation { get; set; }
    }

    public class ObservationInfo
    {
        public string Observation { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class InscriptionSearchResult
    {
        public Guid InscriptionId { get; set; }
        public string CodePostulant { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public string ModalityName { get; set; } = string.Empty;
        public string? TypeModalityName { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public DateTimeOffset InscriptionDate { get; set; }
        public bool CanDownload { get; set; }
        public bool IsModalityActive { get; set; }
        public bool IsMockExam { get; set; }
        public List<FileValidationInfo> Files { get; set; } = new();
        public List<ObservationInfo> Observations { get; set; } = new();
    }

    public record InscriptionFormData(
        IReadOnlyList<NamedOption> Modalities,
        IReadOnlyList<NamedOption> TypePostulants,
        IReadOnlyList<CareerOption> Careers,
        IReadOnlyList<NamedOption> MethodPayments,
        IReadOnlyList<NamedOption> Countries,
        IReadOnlyList<NamedOption> Departments,
        IReadOnlyList<NamedOption> DisabilityTypes,
        IReadOnlyList<NamedOption> Universities,
        IReadOnlyList<NamedOption> CareersAll);

    public record NamedOption(Guid Id, string Name);
    public record CareerOption(Guid Id, string Name, string FacultyName);
    public record SchoolOption(Guid Id, string Name, string? Management, string? Level);

    public record UserAutofillData(
        string? Name,
        string? FirstNameFather,
        string? FirstNameMother,
        DateTimeOffset? Birthdate,
        string? Email,
        string? PhoneNumber,
        string? Genero,
        string? Address,
        Guid? CountryId,
        Guid? DepartmentId,
        Guid? ProvincieId,
        Guid? UbigeoId,
        string? UbigeoCode,
        Guid? SchoolId,
        string? OtherSchool,
        string? SchoolType,
        Guid? SchoolDepartmentId,
        Guid? SchoolProvincieId,
        Guid? SchoolDistritId);

    public record TypeModalityWithKind(Guid Id, string Name, decimal DiscountPercentage, string Kind);

    public record ModalityDates(Guid Id, string Name, string StartDate, string EndDate, string? ExamDate, string? ResultsPublicationDate);

    public record RequirementOption(Guid Id, Guid FileRequirementManagementId, string Name);

    public class PaymentInfoResult
    {
        public bool RequiresPayment { get; init; }
        public decimal? BaseAmount { get; init; }
        public decimal? DiscountPercentage { get; init; }
        public decimal? FinalAmount { get; init; }
        public string? ConceptDescription { get; init; }
        public string? ConceptCode { get; init; }

        public static PaymentInfoResult NoPayment() => new() { RequiresPayment = false };
    }
}
