using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Requirement
{
    [Table("ModalityRequisite", Schema = "Requirement")]
    public class ModalityRequisite
    {
        public Guid Id { get; set; }
        public Guid ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid FileRequirementManagementId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        [ForeignKey("ModalityId")]
        public virtual Modality.Modality? Modality { get; set; }

        [ForeignKey("TypeModalityId")]
        public virtual Modality.TypeModality? TypeModality { get; set; }

        [ForeignKey("FileRequirementManagementId")]
        public virtual FileRequirementManagement? FileRequirementManagement { get; set; }
    }
}
