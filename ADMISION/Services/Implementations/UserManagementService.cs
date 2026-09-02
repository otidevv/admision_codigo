using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Users;
using ADMISION.Models.Shared;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ADMISION.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserManagementService(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IReadOnlyList<Users>> ListAdminUsersAsync(CancellationToken ct = default)
        {
            // Solo usuarios administrativos (los que tienen contraseña).
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRols!)
                .ThenInclude(ur => ur.Rol)
                .Where(u => u.Password != null)
                .OrderBy(u => u.UserName)
                .ToListAsync(ct);
        }

        public async Task<UserFormViewModel?> GetForEditAsync(Guid id, CancellationToken ct = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRols)
                .FirstOrDefaultAsync(u => u.Id == id, ct);

            if (user == null) return null;

            return new UserFormViewModel
            {
                Id = user.Id,
                Name = user.Name,
                FirstNameFather = user.FirstNameFather,
                FirstNameMother = user.FirstNameMother,
                UserName = user.UserName!,
                DocumentType = user.DocumentType,
                Document = user.Document,
                SelectedRoleIds = user.UserRols?.Select(ur => ur.RolsId).ToList() ?? new List<Guid>(),
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Genero = user.Genero,
                CivilStatus = user.CivilStatus,
                Address = user.Address,
                Birthdate = user.Birthdate,
                IsDisabled = user.IsDisabled == AppConstants.Usuarios.Inactivo
            };
        }

        public async Task<bool> IsUserNameTakenAsync(string username, CancellationToken ct = default)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username, ct);
        }

        public async Task<UserFormViewModel?> LookupByDocumentAsync(string document, CancellationToken ct = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Document == document, ct);

            if (user == null) return null;

            return new UserFormViewModel
            {
                Id = user.Id,
                Name = user.Name,
                FirstNameFather = user.FirstNameFather,
                FirstNameMother = user.FirstNameMother,
                UserName = user.UserName!,
                DocumentType = user.DocumentType,
                Document = user.Document,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Genero = user.Genero,
                CivilStatus = user.CivilStatus,
                Address = user.Address,
                Birthdate = user.Birthdate
            };
        }

        public async Task<SaveResult> SaveAsync(UserFormViewModel model, string actor, CancellationToken ct = default)
        {
            Users? user;
            bool isNew = false;

            if (model.Id.HasValue && model.Id != Guid.Empty)
            {
                user = await _context.Users.Include(u => u.UserRols).FirstOrDefaultAsync(u => u.Id == model.Id.Value, ct);
                if (user == null) return SaveResult.NotFoundResult();
            }
            else
            {
                if (await _context.Users.AnyAsync(u => u.UserName == model.UserName, ct))
                {
                    return SaveResult.Invalid(new ValidationError(nameof(model.UserName), "El nombre de usuario ya está en uso."));
                }

                if (!string.IsNullOrEmpty(model.Document) && await _context.Users.AnyAsync(u => u.Document == model.Document, ct))
                {
                    return SaveResult.Invalid(new ValidationError(nameof(model.Document), "El número de documento ya está registrado en otro usuario."));
                }

                user = new Users { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
                isNew = true;
            }

            user.Name = model.Name;
            user.FirstNameFather = model.FirstNameFather;
            user.FirstNameMother = model.FirstNameMother;
            user.FullName = $"{model.Name}, {model.FirstNameFather} {model.FirstNameMother}".Trim();
            user.UserName = model.UserName;
            user.DocumentType = model.DocumentType ?? "DNI";
            user.Document = model.Document ?? "";
            user.PhoneNumber = model.PhoneNumber ?? "";
            user.Email = model.Email ?? "";
            user.Genero = model.Genero ?? "";
            user.CivilStatus = model.CivilStatus;
            user.Address = model.Address;
            user.Birthdate = model.Birthdate?.ToUniversalTime() ?? DateTimeOffset.MinValue;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.UpdatedBy = actor;

            if (!string.IsNullOrEmpty(model.Password))
            {
                user.Password = _passwordHasher.HashPassword(model.Password);
                if (!isNew)
                {
                    user.TokenVersion++;
                }
            }
            else if (isNew)
            {
                return SaveResult.Invalid(new ValidationError(nameof(model.Password), "La contraseña es requerida para nuevos usuarios."));
            }

            if (isNew)
            {
                user.IsDisabled = AppConstants.Usuarios.Activo;
                user.CreatedBy = actor;
                _context.Users.Add(user);
            }
            else if (user.IsDisabled != AppConstants.Usuarios.Bloqueado)
            {
                user.IsDisabled = model.IsDisabled
                    ? AppConstants.Usuarios.Inactivo
                    : AppConstants.Usuarios.Activo;
            }

            // Sincronizar roles: añadir nuevos, quitar deseleccionados.
            if (model.SelectedRoleIds != null)
            {
                var toRemove = user.UserRols?.Where(ur => !model.SelectedRoleIds.Contains(ur.RolsId)).ToList();
                if (toRemove != null) _context.UserRols.RemoveRange(toRemove);

                var existingRoleIds = user.UserRols?.Select(ur => ur.RolsId).ToList() ?? new List<Guid>();
                foreach (var roleId in model.SelectedRoleIds.Where(rid => !existingRoleIds.Contains(rid)))
                {
                    _context.UserRols.Add(new UserRol
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RolsId = roleId
                    });
                }
            }

            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<bool> ToggleBlockAsync(Guid userId, string? reason, string actor, CancellationToken ct = default)
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, ct);
            if (user == null) return false;

            if (user.IsDisabled == AppConstants.Usuarios.Bloqueado)
            {
                user.IsDisabled = AppConstants.Usuarios.Activo;
            }
            else
            {
                user.IsDisabled = AppConstants.Usuarios.Bloqueado;
                if (!string.IsNullOrEmpty(reason))
                {
                    _context.UserObservations.Add(new Observations
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Observation = reason,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = actor
                    });
                }

                // Revocar todos los tokens API activos e incrementar versión
                user.TokenVersion++;
                await _context.ApiTokens
                    .Where(t => t.UserId == user.Id && !t.IsRevoked)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.IsRevoked, true)
                        .SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow),
                        ct);
            }

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        {
            var exists = await _context.UserRols.AnyAsync(ur => ur.UserId == userId && ur.RolsId == roleId, ct);
            if (exists) return true;

            _context.UserRols.Add(new UserRol
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RolsId = roleId
            });
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        {
            var userRol = await _context.UserRols.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RolsId == roleId, ct);
            if (userRol == null) return true;

            _context.UserRols.Remove(userRol);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<UserDeleteOutcome> DeleteAsync(Guid id, string currentUserName, CancellationToken ct = default)
        {
            var user = await _context.Users
                .Include(u => u.UserRols)
                .FirstOrDefaultAsync(u => u.Id == id, ct);

            if (user == null) return UserDeleteOutcome.NotFound;
            if (user.UserName == currentUserName) return UserDeleteOutcome.SelfDeletion;

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(ct);
                return UserDeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return UserDeleteOutcome.HasDependencies;
            }
        }

        public async Task<IReadOnlyList<RoleOption>> GetActiveRolesAsync(CancellationToken ct = default)
        {
            return await _context.Rols
                .AsNoTracking()
                .Where(r => r.State)
                .Select(r => new RoleOption(r.Id, r.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<PasswordResetCandidate>> ListPasswordResetCandidatesAsync(CancellationToken ct = default)
        {
            var adminRoles = AdminRoleNames;
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Password != null
                            && u.IsDisabled == AppConstants.Usuarios.Activo
                            && !string.IsNullOrWhiteSpace(u.Email)
                            && u.UserRols!.Any(ur => ur.Rol != null
                                                     && ur.Rol.State
                                                     && adminRoles.Contains(ur.Rol.Name)))
                .OrderBy(u => u.UserName)
                .Select(u => new PasswordResetCandidate(u.Id, u.UserName!, u.FullName, u.Email!))
                .ToListAsync(ct);
        }

        public async Task<PasswordResetResult> ResetPasswordAsync(Guid userId, string actor, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
            {
                return new PasswordResetResult(false, "El usuario no existe.", null, null, null, null);
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new PasswordResetResult(false, "El usuario no tiene un correo electrónico registrado.", null, user.UserName, user.FullName, null);
            }

            var temp = GenerateTemporaryPassword();
            user.Password = _passwordHasher.HashPassword(temp);
            user.TokenVersion++;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);

            return new PasswordResetResult(true, null, temp, user.UserName, user.FullName, user.Email);
        }

        private static readonly string[] AdminRoleNames =
        {
            AppConstants.Roles.Admin,
            AppConstants.Roles.Soporte,
            AppConstants.Roles.SuperAdmin
        };

        private static string GenerateTemporaryPassword(int length = 10)
        {
            const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var buffer = new char[length];
            for (var i = 0; i < length; i++)
            {
                buffer[i] = allowed[RandomNumberGenerator.GetInt32(allowed.Length)];
            }
            return new string(buffer);
        }

        public async Task<UserProfileDetailViewModel?> GetProfileAsync(Guid userId, int? year, int? month, CancellationToken ct = default)
        {
            var user = await _context.Users.AsNoTracking()
                .Include(u => u.UserRols!).ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null) return null;

            var userIdString = userId.ToString();

            // Accesos por UserId o UserName (el primero puede haber quedado nulo en logs antiguos).
            var accessQuery = _context.AccessLogs.AsNoTracking()
                .Where(a => a.UserId == userIdString || a.UserName == user.UserName);

            var totalSuccess = await accessQuery.CountAsync(a => a.Status == "Success", ct);
            var totalFailure = await accessQuery.CountAsync(a => a.Status != "Success", ct);

            var lastLoginLog = await accessQuery
                .Where(a => a.Status == "Success")
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync(ct);

            var recent = await accessQuery
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .Select(a => new AccessLogItem
                {
                    Timestamp = a.Timestamp,
                    Action = a.Action,
                    Status = a.Status,
                    IpAddress = a.IpAddress,
                    Details = a.Details,
                    ResponseCode = a.ResponseCode
                })
                .ToListAsync(ct);

            var years = await accessQuery
                .Where(a => a.Status == "Success")
                .Select(a => a.Timestamp.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync(ct);

            var selectedYear = year ?? (years.FirstOrDefault() == 0 ? DateTime.Now.Year : years.First());
            var selectedMonth = month ?? DateTime.Now.Month;
            if (selectedMonth < 1 || selectedMonth > 12) selectedMonth = DateTime.Now.Month;

            var monthBuckets = await accessQuery
                .Where(a => a.Status == "Success" && a.Timestamp.Year == selectedYear)
                .GroupBy(a => a.Timestamp.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var byMonth = new int[12];
            foreach (var b in monthBuckets) byMonth[b.Month - 1] = b.Count;

            var daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);
            var dayBuckets = await accessQuery
                .Where(a => a.Status == "Success"
                            && a.Timestamp.Year == selectedYear
                            && a.Timestamp.Month == selectedMonth)
                .GroupBy(a => a.Timestamp.Day)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var byDay = new int[daysInMonth];
            foreach (var b in dayBuckets) if (b.Day >= 1 && b.Day <= daysInMonth) byDay[b.Day - 1] = b.Count;

            var viewed = await _context.NotificationViews.AsNoTracking()
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.ViewedAt)
                .Take(50)
                .Select(v => new NotificationViewedItem
                {
                    NotificationId = v.NotificationId,
                    Title = v.Notification!.Title,
                    Message = v.Notification.Message,
                    IconClass = v.Notification.IconClass,
                    ColorScheme = v.Notification.ColorScheme,
                    CreatedAt = v.Notification.CreatedAt,
                    ViewedAt = v.ViewedAt
                })
                .ToListAsync(ct);

            var totalNotifViewed = await _context.NotificationViews.CountAsync(v => v.UserId == userId, ct);

            return new UserProfileDetailViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Document = user.Document,
                DocumentType = user.DocumentType,
                PhotoUrl = user.PhotoUrl,
                Status = string.IsNullOrEmpty(user.IsDisabled) ? "Activo" : user.IsDisabled,
                Roles = user.UserRols?.Select(ur => ur.Rol?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new(),
                CreatedAt = user.CreatedAt,
                LastLogin = lastLoginLog?.Timestamp,
                LastLoginIp = lastLoginLog?.IpAddress,
                TotalAccessSuccess = totalSuccess,
                TotalAccessFailure = totalFailure,
                TotalNotificationsViewed = totalNotifViewed,
                RecentAccess = recent,
                NotificationsViewed = viewed,
                SelectedYear = selectedYear,
                SelectedMonth = selectedMonth,
                DaysInSelectedMonth = daysInMonth,
                AvailableYears = years.Count > 0 ? years : new List<int> { DateTime.Now.Year },
                LoginsByMonth = byMonth.ToList(),
                LoginsByDayOfSelectedMonth = byDay.ToList()
            };
        }
    }
}
