using ADMISION.Models.Shared;
using ADMISION.Models.ViewModels.Admin;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Perfil del usuario actualmente autenticado: lectura, actualización de
    /// datos básicos y cambio de contraseña.
    /// </summary>
    public interface IProfileService
    {
        Task<ProfileViewModel?> GetProfileAsync(Guid userId, CancellationToken ct = default);
        Task<SaveResult> UpdateAsync(Guid userId, ProfileViewModel input, string actor, CancellationToken ct = default);
        Task<ChangePasswordOutcome> ChangePasswordAsync(Guid userId, ChangePasswordViewModel input, string actor, CancellationToken ct = default);
    }

    public enum ChangePasswordOutcome
    {
        Success,
        UserNotFound,
        WrongCurrentPassword,
        SameAsCurrent
    }
}
