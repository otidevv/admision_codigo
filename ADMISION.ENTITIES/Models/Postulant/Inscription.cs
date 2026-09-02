using System.ComponentModel.DataAnnotations.Schema;

using ADMISION.ENTITIES.Models.Postulant;

namespace ADMISION.ENTITIES.Models.Postulante
{
    [Table("Inscription", Schema = "Postulant")]
    public class Inscription
    {
        public Guid Id { get; set; }
        public string CodePostulant { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool IsAdmission { get; set; }
        public decimal? GradeAdmission { get; set; }
        public Guid? SchoolId { get; set; }
        public Guid? DistritId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid CountryId { get; set; }
        public Guid PostulantId { get; set; }
        public Guid CareerId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? TypePostulantInscriptionId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;
        public string? OtherSchool { get; set; }
        public bool DJ { get; set; }
        public string? SchoolType { get; set; }             // "Publico" o "Privado"
        public string? EducationalLevel { get; set; }       // "Primaria" o "Secundaria"
        public string? Grade { get; set; }                  // "1"–"6" para Primaria, "1"–"5" para Secundaria
        public int? InscriptionOrder { get; set; }          // Orden de mérito de ingreso

        // Datos de traslado (se llenan sólo si el tipo de modalidad es Traslado Externo/Interno)
        public Guid? SourceUniversityId { get; set; }   // Traslado externo: universidad de origen (catálogo)
        public string? SourceCareerName { get; set; }   // Traslado externo: carrera de origen (texto libre)
        public Guid? SourceCareerId { get; set; }       // Traslado interno: carrera de origen (catálogo interno)

        public virtual ADMISION.ENTITIES.Models.Postulant.Postulant? Postulant { get; set; }
        [ForeignKey("CareerId")]
        public virtual Modality.Career? Career { get; set; }
        [ForeignKey("SchoolId")]
        public virtual Schools.Schools? School { get; set; }
        [ForeignKey("DistritId")]
        public virtual Ubigeo.Distrit? Distrit { get; set; }
        [ForeignKey("CountryId")]
        public virtual Ubigeo.Country? Country { get; set; }
        [ForeignKey("ModalityId")]
        public virtual Modality.Modality? Modality { get; set; }
        [ForeignKey("TypeModalityId")]
        public virtual Modality.TypeModality? TypeModality { get; set; }
        [ForeignKey("TypePostulantInscriptionId")]
        public virtual ADMISION.ENTITIES.Models.Postulant.TypePostulantInscription? TypePostulantInscription { get; set; }

        [ForeignKey("SourceUniversityId")]
        public virtual Info.University? SourceUniversity { get; set; }

        [ForeignKey("SourceCareerId")]
        public virtual Modality.Career? SourceCareer { get; set; }

        public virtual ICollection<EconomicManagement.Payments>? Payments { get; set; }
        public virtual ICollection<FileSubmission>? FileSubmissions { get; set; }
        public virtual ICollection<Models.Postulant.Observations>? Observations { get; set; }
        public virtual ICollection<Resignation>? Resignations { get; set; }
        public virtual ICollection<Parent>? Parents { get; set; }
    }
}
