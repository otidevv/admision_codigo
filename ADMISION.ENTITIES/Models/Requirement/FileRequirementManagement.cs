using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Requirement
{
    [Table("FileRequirementManagement", Schema = "Requirement")]
    public class FileRequirementManagement
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePathExtencion { get; set; } = string.Empty;
        public decimal MaxFileSizeMB { get; set; }
        public string Stage { get; set; } = "Postulation"; // Postulation | Entry | Both
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        public virtual ICollection<ModalityRequisite>? ModalityRequisites { get; set; }
        public virtual ICollection<Postulant.FileSubmission>? FileSubmissions { get; set; }

    }
}
