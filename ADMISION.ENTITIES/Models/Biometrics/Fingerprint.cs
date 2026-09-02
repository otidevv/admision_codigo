using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Biometrics
{
    [Table("Fingerprints", Schema = "Biometrics")]
    public class Fingerprint
    {
        public Guid Id { get; set; }
        public Guid? PostulantId { get; set; }
        public int FingerIndex { get; set; } // 0-9
        public string Template { get; set; } = string.Empty; // Base64 encoded ZK template
        public string? ImageBase64 { get; set; }              // BMP image of the fingerprint (Base64)
        public string? DeviceIp { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("PostulantId")]
        public virtual Postulant.Postulant? Postulant { get; set; }
    }
}
