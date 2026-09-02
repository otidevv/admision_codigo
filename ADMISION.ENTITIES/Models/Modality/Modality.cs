using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("Modality", Schema = "Modality")]
    public class Modality
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PublicSummary { get; set; }
        public string? IconKey { get; set; }
        public string? Badge { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public int Orden { get; set; }
        public bool IsCepreExam { get; set; }
        public bool RequiresProfilePhoto { get; set; }
        public bool IsMockExam { get; set; }
        public bool RequiresSchoolType { get; set; }             // Preguntar tipo de gestión (público/privado)
        public bool RequiresEducationalLevel { get; set; }       // Preguntar nivel educativo (primaria/secundaria)
        public bool RequiresGrade { get; set; }                  // Preguntar grado
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        // Hora de apertura/cierre de la inscripción. Junto con StartDate/EndDate
        // define el momento exacto (date + time) en que el proceso abre/cierra.
        public TimeOnly StartTime { get; set; } = new TimeOnly(0, 0);
        public TimeOnly EndTime { get; set; } = new TimeOnly(23, 59, 59);
        public DateOnly? ExamDate { get; set; }
        public DateOnly? ResultsPublicationDate { get; set; }

        // Número inicial del correlativo de código de postulante (ej: "0800000").
        // Se guarda como string para preservar ceros a la izquierda; sólo se aceptan dígitos.
        // El largo de esta cadena define el padding del código generado por postulante.
        public string? StartingCode { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid TermId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }
        public virtual ICollection<Models.Modality.Vacancies>? Vacancies { get; set; }
        public virtual ICollection<Models.Requirement.ModalityRequisite>? ModalityRequisites { get; set; }
        public virtual ICollection<ModalityCareer>? ModalityCareers { get; set; }

    }
}
