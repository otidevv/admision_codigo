using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.ENTITIES.Models.Exam
{
    /// <summary>
    /// Perfil de calificación de un examen. Soporta dos modos:
    /// - Simple: puntajes planos por pregunta (correcta / blanco / incorrecta).
    /// - Ponderado (IsWeighted): los rangos (<see cref="ScoringProfileRange"/>) definen
    ///   el puntaje por correcta de cada bloque de preguntas.
    /// </summary>
    [Table("ScoringProfile", Schema = "Exam")]
    public class ScoringProfile
    {
        public Guid Id { get; set; }

        [MaxLength(180)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>true = calificación ponderada por rangos; false = puntaje plano.</summary>
        public bool IsWeighted { get; set; }

        public decimal PuntosCorrecta { get; set; }
        public decimal PuntosBlanco { get; set; }
        public decimal PuntosIncorrecta { get; set; }
        public decimal NotaMinimaIngreso { get; set; }

        public bool AplicarVigesimal { get; set; }

        /// <summary>Ignorar | Redistribuir | Descontar</summary>
        [MaxLength(20)]
        public string ManejoAnuladas { get; set; } = "Ignorar";

        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? CareerId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }

        [ForeignKey("ModalityId")]
        public virtual Modality.Modality? Modality { get; set; }

        [ForeignKey("TypeModalityId")]
        public virtual TypeModality? TypeModality { get; set; }

        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }

        public virtual ICollection<ScoringProfileRange> Ranges { get; set; } = new List<ScoringProfileRange>();
    }
}
