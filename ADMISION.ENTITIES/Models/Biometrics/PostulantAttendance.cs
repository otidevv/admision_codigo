using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Biometrics
{
    [Table("PostulantAttendance", Schema = "Biometrics")]
    public class PostulantAttendance
    {
        public Guid Id { get; set; }
        
        public Guid InscriptionId { get; set; }
        
        /// <summary>
        /// Estado de la verificación biométrica: "Verificado", "Fallido", "Manual"
        /// </summary>
        public string BiometricStatus { get; set; } = string.Empty;
        
        /// <summary>
        /// Puntuación de coincidencia retornada por el SDK ZK (si aplica)
        /// </summary>
        public int? BiometricScore { get; set; }
        
        public DateTimeOffset VerifiedAt { get; set; } = DateTimeOffset.UtcNow;
        public string VerifiedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }

        [ForeignKey("InscriptionId")]
        public virtual Postulante.Inscription? Inscription { get; set; }
    }
}
