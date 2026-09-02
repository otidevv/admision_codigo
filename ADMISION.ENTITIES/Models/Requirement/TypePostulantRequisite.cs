using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Requirement
{
    [Table("TypePostulantRequisite", Schema = "Requirement")]
    public class TypePostulantRequisite
    {
        public Guid Id { get; set; }
        public Guid TypePostulantInscriptionId { get; set; }
        public Guid FileRequirementManagementId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        [ForeignKey("TypePostulantInscriptionId")]
        public virtual Postulant.TypePostulantInscription? TypePostulantInscription { get; set; }

        [ForeignKey("FileRequirementManagementId")]
        public virtual FileRequirementManagement? FileRequirementManagement { get; set; }
    }
}
