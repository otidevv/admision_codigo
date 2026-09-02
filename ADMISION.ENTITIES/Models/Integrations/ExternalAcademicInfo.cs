using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Integrations
{
    [Table("ExternalAcademicInfo", Schema = "Integrations")]
    public class ExternalAcademicInfo
    {
        public Guid Id { get; set; }

        public Guid ExternalApiId { get; set; }
        [ForeignKey("ExternalApiId")]
        public virtual ExternalApi? ExternalApi { get; set; }

        public string Dni { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PaternalSurname { get; set; } = string.Empty;
        public string MaternalSurname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PersonalEmail { get; set; }
        public string CareerName { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public decimal TotalCreditsApproved { get; set; }

        public Guid QueryLogId { get; set; }
        [ForeignKey("QueryLogId")]
        public virtual ApiQueryLog? QueryLog { get; set; }

        public DateTimeOffset QueriedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
