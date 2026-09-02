using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Users;
using ADMISION.Models.Shared;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class TeacherService : ITeacherService
    {
        private readonly AppDbContext _context;

        public TeacherService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Teachers>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Teachers
                .AsNoTracking()
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<Teachers?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public async Task<TeacherFormViewModel?> GetForEditAsync(Guid id, CancellationToken ct = default)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (teacher?.User == null) return null;

            return new TeacherFormViewModel
            {
                Id = teacher.Id,
                UserId = teacher.UserId,
                Specialization = teacher.Specialization,
                Degree = teacher.Degree,
                Type = teacher.Type,
                IsActive = teacher.IsActive,
                Name = teacher.User.Name,
                FirstNameFather = teacher.User.FirstNameFather,
                FirstNameMother = teacher.User.FirstNameMother,
                DocumentType = teacher.User.DocumentType,
                Document = teacher.User.Document,
                PhoneNumber = teacher.User.PhoneNumber,
                Email = teacher.User.Email,
                Genero = teacher.User.Genero,
                Birthdate = teacher.User.Birthdate,
                Address = teacher.User.Address
            };
        }

        public async Task<SaveResult> SaveAsync(TeacherFormViewModel model, string actor, CancellationToken ct = default)
        {
            var fullName = $"{model.Name} {model.FirstNameFather} {model.FirstNameMother}".Trim();

            if (model.Id.HasValue && model.Id != Guid.Empty && model.UserId.HasValue)
            {
                // Edit existing
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == model.Id.Value, ct);

                if (teacher == null) return SaveResult.NotFoundResult();
                if (teacher.User == null) return SaveResult.NotFoundResult();

                teacher.Specialization = model.Specialization;
                teacher.Degree = model.Degree;
                teacher.Type = model.Type;
                teacher.IsActive = model.IsActive;
                teacher.UpdatedAt = DateTimeOffset.UtcNow;
                teacher.UpdatedBy = actor;

                teacher.User.Name = model.Name;
                teacher.User.FirstNameFather = model.FirstNameFather;
                teacher.User.FirstNameMother = model.FirstNameMother;
                teacher.User.FullName = fullName;
                teacher.User.DocumentType = model.DocumentType;
                teacher.User.Document = model.Document;
                teacher.User.PhoneNumber = model.PhoneNumber;
                teacher.User.Email = model.Email;
                teacher.User.Genero = model.Genero;
                teacher.User.Address = model.Address;
                teacher.User.UpdatedAt = DateTimeOffset.UtcNow;
                teacher.User.UpdatedBy = actor;

                if (model.Birthdate.HasValue)
                    teacher.User.Birthdate = model.Birthdate.Value.ToUniversalTime();
            }
            else
            {
                // Create new
                var user = new Users
                {
                    Id = Guid.NewGuid(),
                    Name = model.Name,
                    FirstNameFather = model.FirstNameFather,
                    FirstNameMother = model.FirstNameMother,
                    FullName = fullName,
                    DocumentType = model.DocumentType,
                    Document = model.Document,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    Genero = model.Genero,
                    Address = model.Address,
                    Birthdate = model.Birthdate?.ToUniversalTime() ?? new DateTimeOffset(DateTime.MinValue, TimeSpan.Zero),
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = actor
                };

                var teacher = new Teachers
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Specialization = model.Specialization,
                    Degree = model.Degree,
                    Type = model.Type,
                    IsActive = model.IsActive,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = actor
                };

                _context.Users.Add(user);
                _context.Teachers.Add(teacher);
            }

            try
            {
                await _context.SaveChangesAsync(ct);
                return SaveResult.Ok();
            }
            catch (DbUpdateException)
            {
                return SaveResult.Invalid(new ValidationError(string.Empty, "No se pudieron guardar los datos. Verifique que el documento no esté duplicado."));
            }
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == id, ct);

                if (teacher == null) return DeleteOutcome.NotFound;

                _context.Teachers.Remove(teacher);
                if (teacher.User != null)
                    _context.Users.Remove(teacher.User);

                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        public async Task<Teachers?> ToggleActiveAsync(Guid id, string actor, CancellationToken ct = default)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (teacher == null) return null;

            teacher.IsActive = !teacher.IsActive;
            teacher.UpdatedAt = DateTimeOffset.UtcNow;
            teacher.UpdatedBy = actor;

            await _context.SaveChangesAsync(ct);
            return teacher;
        }

        public async Task<bool> ExistsDocumentAsync(string document, Guid? excludeId = null, CancellationToken ct = default)
        {
            return await _context.Teachers
                .AsNoTracking()
                .Include(t => t.User)
                .AnyAsync(t => t.User!.Document == document && (!excludeId.HasValue || t.Id != excludeId.Value), ct);
        }

        public async Task<TeacherImportResult> ImportFromExcelAsync(Stream excelStream, string actor, CancellationToken ct = default)
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            var existingDocs = await _context.Users
                .AsNoTracking()
                .Select(u => u.Document)
                .ToListAsync(ct);

            var existingTeacherUserIds = await _context.Teachers
                .AsNoTracking()
                .Select(t => t.UserId)
                .ToListAsync(ct);

            var newUsers = new List<Users>();
            var newTeachers = new List<Teachers>();
            var errors = new List<TeacherImportError>();
            var importedCount = 0;

            foreach (var row in rows)
            {
                var dni = row.Cell(1).GetValue<string>()?.Trim() ?? "";
                var apPaterno = row.Cell(2).GetValue<string>()?.Trim() ?? "";
                var apMaterno = row.Cell(3).GetValue<string>()?.Trim() ?? "";
                var nombres = row.Cell(4).GetValue<string>()?.Trim() ?? "";
                var especialidad = row.Cell(5).GetValue<string>()?.Trim() ?? "";
                var grado = row.Cell(6).GetValue<string>()?.Trim() ?? "";
                var tipo = row.Cell(7).GetValue<string>()?.Trim() ?? "";

                var importRow = new TeacherImportRow(dni, apPaterno, apMaterno, nombres, especialidad, grado, tipo);

                if (string.IsNullOrEmpty(dni))
                {
                    errors.Add(new TeacherImportError(importRow, "El DNI es requerido."));
                    continue;
                }
                if (string.IsNullOrEmpty(apPaterno))
                {
                    errors.Add(new TeacherImportError(importRow, "El apellido paterno es requerido."));
                    continue;
                }
                if (string.IsNullOrEmpty(apMaterno))
                {
                    errors.Add(new TeacherImportError(importRow, "El apellido materno es requerido."));
                    continue;
                }
                if (string.IsNullOrEmpty(nombres))
                {
                    errors.Add(new TeacherImportError(importRow, "Los nombres son requeridos."));
                    continue;
                }
                if (string.IsNullOrEmpty(especialidad))
                {
                    errors.Add(new TeacherImportError(importRow, "La especialidad es requerida."));
                    continue;
                }
                if (string.IsNullOrEmpty(grado))
                {
                    errors.Add(new TeacherImportError(importRow, "El grado académico es requerido."));
                    continue;
                }
                if (tipo != "Nombrado" && tipo != "Contratado" && tipo != "Auxiliar")
                {
                    errors.Add(new TeacherImportError(importRow, "El tipo de docente debe ser Nombrado, Contratado o Auxiliar."));
                    continue;
                }

                var fullName = $"{nombres} {apPaterno} {apMaterno}".Trim();

                Guid userId;
                if (existingDocs.Contains(dni))
                {
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Document == dni, ct);
                    if (existingUser == null)
                    {
                        errors.Add(new TeacherImportError(importRow, "Error al obtener el usuario existente."));
                        continue;
                    }
                    userId = existingUser.Id;
                }
                else
                {
                    userId = Guid.NewGuid();
                    var user = new Users
                    {
                        Id = userId,
                        Name = nombres,
                        FirstNameFather = apPaterno,
                        FirstNameMother = apMaterno,
                        FullName = fullName,
                        DocumentType = "DNI",
                        Document = dni,
                        PhoneNumber = "",
                        Email = "",
                        Genero = "",
                        Address = null,
                        Birthdate = new DateTimeOffset(DateTime.MinValue, TimeSpan.Zero),
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = actor
                    };
                    newUsers.Add(user);
                    existingDocs.Add(dni);
                }

                if (existingTeacherUserIds.Contains(userId))
                {
                    errors.Add(new TeacherImportError(importRow, "Ya existe un docente registrado con ese DNI."));
                    continue;
                }

                var teacher = new Teachers
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Specialization = especialidad,
                    Degree = grado,
                    Type = tipo,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = actor
                };
                newTeachers.Add(teacher);
                existingTeacherUserIds.Add(userId);
                importedCount++;
            }

            if (newUsers.Any())
                _context.Users.AddRange(newUsers);

            if (newTeachers.Any())
                _context.Teachers.AddRange(newTeachers);

            if (newUsers.Any() || newTeachers.Any())
                await _context.SaveChangesAsync(ct);

            return new TeacherImportResult
            {
                ImportedCount = importedCount,
                Errors = errors
            };
        }
    }
}
