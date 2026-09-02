using System.ComponentModel.DataAnnotations;

namespace ADMISION.Models.ViewModels.Public
{
    public class EnrollmentViewModel
    {
        // --- SECTION 1: MAIN DATA ---
        [Required(ErrorMessage = "La nacionalidad es requerida")]
        public string Nationality { get; set; } = "Nacional"; // "Nacional" or "Extranjero"

        [Required(ErrorMessage = "El tipo de documento es requerido")]
        public string DocumentType { get; set; } // DNI, CE, PASAPORTE

        [Required(ErrorMessage = "El número de documento es requerido")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "El documento debe tener entre 8 y 20 caracteres")]
        public string DocumentNumber { get; set; } = string.Empty;

        public Guid ModalityId { get; set; }
        public Guid? SchoolId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid TypePostulantId { get; set; }

        [Required(ErrorMessage = "El celular es requerido")]
        [Phone(ErrorMessage = "Formato de celular inválido")]
        public string PhoneNumber { get; set; }

        // For validation or payment proof
        public Guid? MethodPaymentId { get; set; }

        public decimal PaymentAmount { get; set; }

        public string? PaymentCode { get; set; }
        public IFormFile? PaymentVoucher { get; set; }

        public IFormFile? ProfilePhoto { get; set; }

        // SECTION 2: MANDATORY DOCUMENTS (Captured dynamically in controller)
        // Captured via Request.Form.Files with prefix 'Requirements_'

        // --- SECTION 3: GENERAL DATA ---
        [Required(ErrorMessage = "La carrera es requerida")]
        public Guid CareerId { get; set; }

        // Personal Data
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "El apellido paterno es requerido")]
        public string FatherSurname { get; set; } // Mapping to FirstNameFather
        
        [Required(ErrorMessage = "El apellido materno es requerido")]
        public string MotherSurname { get; set; } // Mapping to FirstNameMother

        public Guid CountryId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? ProvincieId { get; set; }
        public Guid? UbigeoId { get; set; } // This will be the DistritId
        public string? Address { get; set; }
        
        [Required(ErrorMessage = "La fecha de nacimiento es requerida")]
        public DateTime BirthDate { get; set; }
        
        [Required(ErrorMessage = "El género es requerido")]
        public string Genero { get; set; } = "M"; // M or F
        
        public string CivilStatus { get; set; }
        public List<Guid>? DisabilityTypeIds { get; set; } = new List<Guid>();
        public string? ConadisNumber { get; set; }
        public string? Email { get; set; }

        // Institution/School Data
        public string? SchoolName { get; set; }
        public string? SchoolLevel { get; set; } // Secundaria, etc.
        public Guid? SchoolUbigeoId { get; set; }
        [Required(ErrorMessage = "La gestión educativa es requerida")]
        public string SchoolType { get; set; } = "Publico";
        public string? EducationalLevel { get; set; }   // "Primaria" o "Secundaria"
        public string? Grade { get; set; }              // "1"–"6" para Primaria, "1"–"5" para Secundaria
        public bool IsOutsidePeru { get; set; }
        public Guid? SchoolDepartmentId { get; set; }
        public Guid? SchoolProvincieId { get; set; }
        public Guid? SchoolDistritId { get; set; }
        public string? OtherSchool { get; set; }

        // Guardian Data (if minor) — persisted into Postulant.Parent
        public string? GuardianName { get; set; }
        public string? GuardianFatherSurname { get; set; }
        public string? GuardianMotherSurname { get; set; }
        public string? GuardianDni { get; set; }
        public string? GuardianPhone { get; set; }
        public string? GuardianEmail { get; set; }

        // Datos de traslado (opcionales — validados en el server sólo si aplica)
        public Guid? SourceUniversityId { get; set; }  // Traslado Externo
        public string? SourceCareerName { get; set; }  // Traslado Externo (texto libre)
        public Guid? SourceCareerId { get; set; }      // Traslado Interno (selección)

        // Terms
        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar la declaración jurada")]
        public bool TermsAccepted { get; set; }
    }
}
