using System.ComponentModel.DataAnnotations;

namespace ADMISION.Models.ViewModels.Admin
{
    public class TeacherFormViewModel
    {
        // Teacher fields
        public Guid? Id { get; set; }
        public Guid? UserId { get; set; }

        [Required(ErrorMessage = "La especialidad es requerida.")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "El grado académico es requerido.")]
        public string Degree { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de docente es requerido.")]
        public string Type { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // User personal data
        [Required(ErrorMessage = "El nombre es requerido.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es requerido.")]
        public string FirstNameFather { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido materno es requerido.")]
        public string FirstNameMother { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es requerido.")]
        public string DocumentType { get; set; } = "DNI";

        [Required(ErrorMessage = "El número de documento es requerido.")]
        public string Document { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public DateTimeOffset? Birthdate { get; set; }
        public string? Address { get; set; }
    }
}
