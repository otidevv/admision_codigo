using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class CareerService : ICareerService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public CareerService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        public async Task<PagedResult<CareerListItem>> ListAsync(CareerListQuery query, CancellationToken ct = default)
        {
            var q = _context.Careers
                .AsNoTracking()
                .Include(c => c.Faculty)
                .AsQueryable();

            if (query.FacultyId.HasValue && query.FacultyId.Value != Guid.Empty)
            {
                q = q.Where(c => c.FacultyId == query.FacultyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(c =>
                    EF.Functions.ILike(c.Name, $"%{search}%") ||
                    EF.Functions.ILike(c.Code, $"%{search}%") ||
                    EF.Functions.ILike(c.ProgramNumber ?? "", $"%{search}%"));
            }

            // Ordenar sobre la entidad ANTES de proyectar — EF Core 10 no traduce
            // OrderBy sobre records posicionales (positional record constructor).
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "name" => query.IsDescending ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
                "code" => query.IsDescending ? q.OrderByDescending(c => c.Code) : q.OrderBy(c => c.Code),
                "programnumber" => query.IsDescending ? q.OrderByDescending(c => c.ProgramNumber) : q.OrderBy(c => c.ProgramNumber),
                "faculty" => query.IsDescending ? q.OrderByDescending(c => c.Faculty!.Name) : q.OrderBy(c => c.Faculty!.Name),
                "isactive" => query.IsDescending ? q.OrderByDescending(c => c.IsActive) : q.OrderBy(c => c.IsActive),
                _ => q.OrderBy(c => c.Name)
            };

            var projected = q.Select(c => new CareerListItem(
                c.Id,
                c.Name,
                c.Code,
                c.ProgramNumber,
                c.IsActive,
                c.Faculty != null ? c.Faculty.Name : null));

            return await PagedResult<CareerListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public Task<Career?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Careers
                .Include(c => c.Images!.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<Career> CreateAsync(Career career, CareerFiles files, string actor, CancellationToken ct = default)
        {
            // 1. Pre-validar TODOS los archivos antes de tocar disco o BD. Si alguno
            //    falla (p.ej. extensión inválida en una imagen de galería) abortamos
            //    sin orfanatos: ni filas a medio insertar ni archivos huérfanos.
            await EnsureAllFilesValidAsync(files);

            using var tx = await _context.Database.BeginTransactionAsync(ct);
            var savedFiles = new List<string>();
            try
            {
                if (files.Logo is { Length: > 0 })
                    career.LogoUrl = Track(savedFiles, await _files.SaveFileAsync(files.Logo, "Careers/Logos"));
                if (files.Banner is { Length: > 0 })
                    career.BannerUrl = Track(savedFiles, await _files.SaveFileAsync(files.Banner, "Careers/Banners"));
                if (files.StudyPlan is { Length: > 0 })
                    career.StudyPlanUrl = Track(savedFiles, await _files.SaveFileAsync(files.StudyPlan, "Careers/Plans"));

                career.Id = Guid.NewGuid();
                career.CreatedAt = DateTimeOffset.UtcNow;
                career.CreatedBy = actor;
                _context.Careers.Add(career);

                // Galería en el MISMO SaveChanges → todo se commitea o nada.
                if (files.GalleryImages is { Count: > 0 })
                {
                    int order = -1;
                    foreach (var file in files.GalleryImages)
                    {
                        if (file is null || file.Length == 0) continue;
                        var url = Track(savedFiles, await _files.SaveFileAsync(file, "Careers/Gallery"));
                        if (string.IsNullOrEmpty(url)) continue;
                        _context.CareerImages.Add(new CareerImage
                        {
                            Id = Guid.NewGuid(),
                            CareerId = career.Id,
                            ImageUrl = url,
                            DisplayOrder = ++order,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = actor
                        });
                    }
                }

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return career;
            }
            catch
            {
                try { await tx.RollbackAsync(ct); } catch { /* ignore */ }
                CleanupOrphanFiles(savedFiles);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Career career, CareerFiles files, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Careers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == career.Id, ct);
            if (existing == null) return false;

            // Validar todo arriba: si la galería trae una imagen con extensión inválida,
            // queremos abortar ANTES de borrar el logo/banner anterior.
            await EnsureAllFilesValidAsync(files);

            using var tx = await _context.Database.BeginTransactionAsync(ct);
            var savedFiles = new List<string>();              // si algo truena, borrar estos
            var oldFilesToPurge = new List<string>();         // si todo OK, borrar los previos al commit

            try
            {
                if (files.Logo is { Length: > 0 })
                {
                    career.LogoUrl = Track(savedFiles, await _files.SaveFileAsync(files.Logo, "Careers/Logos"));
                    if (!string.IsNullOrEmpty(existing.LogoUrl)) oldFilesToPurge.Add(existing.LogoUrl);
                }
                else career.LogoUrl = existing.LogoUrl;

                if (files.Banner is { Length: > 0 })
                {
                    career.BannerUrl = Track(savedFiles, await _files.SaveFileAsync(files.Banner, "Careers/Banners"));
                    if (!string.IsNullOrEmpty(existing.BannerUrl)) oldFilesToPurge.Add(existing.BannerUrl);
                }
                else career.BannerUrl = existing.BannerUrl;

                if (files.StudyPlan is { Length: > 0 })
                {
                    career.StudyPlanUrl = Track(savedFiles, await _files.SaveFileAsync(files.StudyPlan, "Careers/Plans"));
                    if (!string.IsNullOrEmpty(existing.StudyPlanUrl)) oldFilesToPurge.Add(existing.StudyPlanUrl);
                }
                else career.StudyPlanUrl = existing.StudyPlanUrl;

                career.CreatedAt = existing.CreatedAt;
                career.CreatedBy = existing.CreatedBy;
                career.UpdatedAt = DateTimeOffset.UtcNow;
                career.UpdatedBy = actor;
                _context.Careers.Update(career);

                if (files.GalleryImages is { Count: > 0 })
                {
                    var nextOrder = await _context.CareerImages
                        .Where(i => i.CareerId == career.Id)
                        .Select(i => (int?)i.DisplayOrder)
                        .MaxAsync(ct) ?? -1;

                    foreach (var file in files.GalleryImages)
                    {
                        if (file is null || file.Length == 0) continue;
                        var url = Track(savedFiles, await _files.SaveFileAsync(file, "Careers/Gallery"));
                        if (string.IsNullOrEmpty(url)) continue;
                        _context.CareerImages.Add(new CareerImage
                        {
                            Id = Guid.NewGuid(),
                            CareerId = career.Id,
                            ImageUrl = url,
                            DisplayOrder = ++nextOrder,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = actor
                        });
                    }
                }

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // Sólo tras commit borramos los archivos previos.
                CleanupOrphanFiles(oldFilesToPurge);
                return true;
            }
            catch
            {
                try { await tx.RollbackAsync(ct); } catch { /* ignore */ }
                CleanupOrphanFiles(savedFiles);
                throw;
            }
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var career = await _context.Careers
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (career == null) return DeleteOutcome.NotFound;

            try
            {
                if (!string.IsNullOrEmpty(career.LogoUrl)) _files.DeleteFile(career.LogoUrl);
                if (!string.IsNullOrEmpty(career.BannerUrl)) _files.DeleteFile(career.BannerUrl);
                if (!string.IsNullOrEmpty(career.StudyPlanUrl)) _files.DeleteFile(career.StudyPlanUrl);

                if (career.Images != null)
                {
                    foreach (var img in career.Images)
                    {
                        if (!string.IsNullOrEmpty(img.ImageUrl)) _files.DeleteFile(img.ImageUrl);
                    }
                }

                _context.Careers.Remove(career);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        public async Task<int> AddImagesAsync(Guid careerId, IEnumerable<IFormFile> files, string actor, CancellationToken ct = default)
        {
            var careerExists = await _context.Careers.AnyAsync(c => c.Id == careerId, ct);
            if (!careerExists) return 0;

            var nextOrder = await _context.CareerImages
                .Where(i => i.CareerId == careerId)
                .Select(i => (int?)i.DisplayOrder)
                .MaxAsync(ct) ?? -1;

            var added = 0;
            foreach (var file in files)
            {
                if (file is null || file.Length == 0) continue;

                var url = await _files.SaveFileAsync(file, "Careers/Gallery");
                if (string.IsNullOrEmpty(url)) continue;

                _context.CareerImages.Add(new CareerImage
                {
                    Id = Guid.NewGuid(),
                    CareerId = careerId,
                    ImageUrl = url,
                    DisplayOrder = ++nextOrder,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = actor
                });
                added++;
            }

            if (added > 0) await _context.SaveChangesAsync(ct);
            return added;
        }

        public async Task<bool> DeleteImageAsync(Guid careerId, Guid imageId, CancellationToken ct = default)
        {
            var image = await _context.CareerImages.FirstOrDefaultAsync(i => i.Id == imageId && i.CareerId == careerId, ct);
            if (image == null) return false;

            if (!string.IsNullOrEmpty(image.ImageUrl)) _files.DeleteFile(image.ImageUrl);

            _context.CareerImages.Remove(image);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        // ── Helpers internos ─────────────────────────────────────────────────
        private async Task EnsureAllFilesValidAsync(CareerFiles files)
        {
            await EnsureFileValidAsync(files.Logo);
            await EnsureFileValidAsync(files.Banner);
            await EnsureFileValidAsync(files.StudyPlan);
            if (files.GalleryImages is { Count: > 0 })
                foreach (var f in files.GalleryImages)
                    await EnsureFileValidAsync(f);
        }

        private async Task EnsureFileValidAsync(IFormFile? file)
        {
            if (file is null || file.Length == 0) return;
            var result = await _files.ValidateFileAsync(file);
            if (!result.IsValid)
                throw new InvalidFileException(file.FileName ?? "archivo", result.Reason);
        }

        private static string Track(List<string> tracker, string path)
        {
            if (!string.IsNullOrEmpty(path)) tracker.Add(path);
            return path;
        }

        private void CleanupOrphanFiles(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                try { _files.DeleteFile(p); } catch { /* best-effort, no relanzar */ }
            }
        }
    }
}
