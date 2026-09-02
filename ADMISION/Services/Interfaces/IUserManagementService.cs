using ADMISION.ENTITIES.Models.Users;
using ADMISION.Models.Shared;
using ADMISION.Models.ViewModels.Admin;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Gestión de usuarios administrativos: listado, CRUD con roles,
    /// bloqueo, asignación de roles y vista de perfil.
    /// </summary>
    public interface IUserManagementService
    {
        Task<IReadOnlyList<Users>> ListAdminUsersAsync(CancellationToken ct = default);
        Task<UserFormViewModel?> GetForEditAsync(Guid id, CancellationToken ct = default);
        Task<UserFormViewModel?> LookupByDocumentAsync(string document, CancellationToken ct = default);
        Task<bool> IsUserNameTakenAsync(string username, CancellationToken ct = default);
        Task<SaveResult> SaveAsync(UserFormViewModel model, string actor, CancellationToken ct = default);

        Task<bool> ToggleBlockAsync(Guid userId, string? reason, string actor, CancellationToken ct = default);
        Task<bool> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);
        Task<bool> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);

        Task<UserDeleteOutcome> DeleteAsync(Guid id, string currentUserName, CancellationToken ct = default);

        Task<IReadOnlyList<RoleOption>> GetActiveRolesAsync(CancellationToken ct = default);

        Task<IReadOnlyList<PasswordResetCandidate>> ListPasswordResetCandidatesAsync(CancellationToken ct = default);
        Task<PasswordResetResult> ResetPasswordAsync(Guid userId, string actor, CancellationToken ct = default);

        Task<UserProfileDetailViewModel?> GetProfileAsync(Guid userId, int? year, int? month, CancellationToken ct = default);
    }

    public record RoleOption(Guid Id, string Name);

    public record PasswordResetCandidate(Guid UserId, string UserName, string FullName, string Email);

    public record PasswordResetResult(bool Success, string? Error, string? TempPassword, string? UserName, string? FullName, string? Email);

    public enum UserDeleteOutcome
    {
        Deleted,
        NotFound,
        SelfDeletion,
        HasDependencies
    }
}
