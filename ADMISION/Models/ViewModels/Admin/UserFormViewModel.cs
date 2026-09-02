using System.ComponentModel.DataAnnotations;

namespace ADMISION.Models.ViewModels.Admin
{
    public class UserFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es requerido")]
        [Display(Name = "Apellido Paterno")]
        public string FirstNameFather { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido materno es requerido")]
        [Display(Name = "Apellido Materno")]
        public string FirstNameMother { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [Display(Name = "Nombre de Usuario")]
        public string UserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmPassword { get; set; }

        public string? DocumentType { get; set; }
        public string? Document { get; set; }

        [Display(Name = "Roles")]
        public List<Guid> SelectedRoleIds { get; set; } = new List<Guid>();

        [Display(Name = "Teléfono")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
        [Display(Name = "Correo Electrónico")]
        public string? Email { get; set; }

        [Display(Name = "Género")]
        public string? Genero { get; set; }

        [Display(Name = "Estado Civil")]
        public string? CivilStatus { get; set; }

        [Display(Name = "Dirección")]
        public string? Address { get; set; }

        [Display(Name = "Fecha de Nacimiento")]
        public DateTimeOffset? Birthdate { get; set; }

        [Display(Name = "Deshabilitar usuario")]
        public bool IsDisabled { get; set; }
    }
}
