using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Exam
{
    /// <summary>
    /// Bloque de preguntas de un perfil ponderado: define el puntaje por
    /// respuesta correcta para el rango [FromQuestion..ToQuestion].
    /// Blanco e incorrecta usan los valores globales del perfil.
    /// </summary>
    [Table("ScoringProfileRange", Schema = "Exam")]
    public class ScoringProfileRange
    {
        public Guid Id { get; set; }
        public Guid ScoringProfileId { get; set; }
        public int FromQuestion { get; set; }
        public int ToQuestion { get; set; }
        public decimal PuntosCorrecta { get; set; }
        public int DisplayOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("ScoringProfileId")]
        public virtual ScoringProfile? Profile { get; set; }
    }
}
