using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Info
{
    /// <summary>
    /// Pregunta frecuente para el chatbot del portal público. El campo
    /// <see cref="Keywords"/> almacena variantes/sinónimos separados por coma,
    /// que el motor de matching tokeniza para encontrar la mejor coincidencia
    /// cuando el usuario escribe una pregunta libre.
    /// </summary>
    [Table("FaqItem", Schema = "Info")]
    public class FaqItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        /// <summary>Categoría agrupadora visible al postulante (Inscripción, Pagos, Examen, …).</summary>
        [MaxLength(80)]
        public string? Category { get; set; }

        /// <summary>Palabras clave / sinónimos separados por coma para mejorar el matching.</summary>
        [MaxLength(500)]
        public string? Keywords { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Contador de veces que la pregunta fue resuelta (matched) por el chatbot.</summary>
        public int HitCount { get; set; }

        /// <summary>Opción padre para navegación jerárquica del chatbot por opciones.</summary>
        public Guid? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public FaqItem? Parent { get; set; }

        public ICollection<FaqItem> Children { get; set; } = new List<FaqItem>();

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;
    }
}
