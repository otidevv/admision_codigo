using ADMISION.ENTITIES.Data;
using ADMISION.Models.Shared;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _hasher;

        public ProfileService(AppDbContext context, IPasswordHasher hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<ProfileViewModel?> GetProfileAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRols!).ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null) return null;

            var role = user.UserRols?.Select(ur => ur.Rol?.Name).FirstOrDefault(r => !string.IsNullOrEmpty(r)) ?? "Usuario";

            return new ProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Document = user.Document,
                DocumentType = user.DocumentType,
                PhotoUrl = user.PhotoUrl,
                Role = role!,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task<SaveResult> UpdateAsync(Guid userId, ProfileViewModel input, string actor, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null) return SaveResult.NotFoundResult();

            // Validar email único cuando cambia.
            var normalizedEmail = input.Email.Trim();
            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var taken = await _context.Users.AnyAsync(
                    u => u.Id != userId && u.Email.ToLower() == normalizedEmail.ToLower(), ct);
                if (taken)
                {
                    return SaveResult.Invalid(new ValidationError(
                        nameof(ProfileViewModel.Email),
                        "Este correo ya está registrado por otro usuario."));
                }
            }

            user.Email = normalizedEmail;
            user.PhoneNumber = input.PhoneNumber.Trim();
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);

            return SaveResult.Ok();
        }

        public async Task<ChangePasswordOutcome> ChangePasswordAsync(Guid userId, ChangePasswordViewModel input, string actor, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null) return ChangePasswordOutcome.UserNotFound;

            if (string.IsNullOrEmpty(user.Password) || !_hasher.VerifyPassword(input.CurrentPassword, user.Password))
                return ChangePasswordOutcome.WrongCurrentPassword;

            if (_hasher.VerifyPassword(input.NewPassword, user.Password))
                return ChangePasswordOutcome.SameAsCurrent;

            user.Password = _hasher.HashPassword(input.NewPassword);
            user.TokenVersion++;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);

            return ChangePasswordOutcome.Success;
        }
    }
}
