using System.ComponentModel.DataAnnotations;

namespace ADMISION.Models.ViewModels.Admin
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }

        // Datos solo lectura (se muestran pero no se editan en POST)
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string Role { get; set; } = string.Empty;

        // Datos editables
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de contacto es obligatorio.")]
        [StringLength(25, ErrorMessage = "El número no puede superar los 25 caracteres.")]
        [RegularExpression(@"^[0-9+\-\s()]+$", ErrorMessage = "Ingrese un número de contacto válido.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese su contraseña actual.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
