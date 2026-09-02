using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

using ADMISION.ENTITIES.Models.Postulante;

namespace ADMISION.ENTITIES.Models.Postulant
{
    [Table("FileSubmission", Schema = "Postulant")]
    public class FileSubmission
    {
        public Guid Id { get; set; }
        public Guid InscriptionId { get; set; }
        public Guid FileRequirementManagementId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;

        /// <summary>Validado por el operador de admisión. False = pendiente de revisión.</summary>
        public bool IsValidated { get; set; }
        public DateTimeOffset? ValidatedAt { get; set; }
        public string? ValidatedBy { get; set; }
        /// <summary>Motivo de rechazo / observación del operador (opcional).</summary>
        public string? ValidationNote { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual Inscription? Inscription { get; set; }
        [ForeignKey("FileRequirementManagementId")]
        public virtual Requirement.FileRequirementManagement? FileRequirementManagement { get; set; }

    }
}
