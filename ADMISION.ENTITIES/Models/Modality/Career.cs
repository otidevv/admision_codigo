using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("Career", Schema = "Modality")]
    public class Career
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? TematicArea { get; set; } // P, Q, R
        public bool IsActive { get; set; } = true;
        public Guid FacultyId { get; set; }

        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? Initials { get; set; }
        public string? ProgramNumber { get; set; }
        public string? StudyPlanUrl { get; set; }
        public int DisplayOrder { get; set; }

        // Información extendida para la página pública de la carrera.
        public int? DurationSemesters { get; set; }
        public string? DegreeTitle { get; set; }          // Título profesional (ej: "Ingeniero de Sistemas")
        public string? AcademicDegree { get; set; }        // Grado académico (ej: "Bachiller en Ingeniería de Sistemas")
        public string? GraduateProfile { get; set; }       // Perfil del egresado (texto largo)
        public string? AdmissionProfile { get; set; }      // Perfil del ingresante (texto largo)
        public string? JobField { get; set; }              // Campo laboral / mercado

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("FacultyId")]
        public virtual Faculty? Faculty { get; set; }
        public virtual ICollection<Models.Postulante.Inscription>? Inscriptions { get; set; }
        public virtual ICollection<CareerImage>? Images { get; set; }

    }
}
