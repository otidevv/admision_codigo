using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Schools;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class SchoolService : ISchoolService
    {
        private readonly AppDbContext _context;

        public SchoolService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<SchoolListItem>> ListAsync(SchoolListQuery query, CancellationToken ct = default)
        {
            var q = _context.Schools
                .AsNoTracking()
                .Include(s => s.Distrit).ThenInclude(d => d!.Province).ThenInclude(p => p!.Department)
                .AsQueryable();

            if (query.DepartmentId.HasValue)
                q = q.Where(s => s.Distrit != null && s.Distrit.Province != null && s.Distrit.Province.DepartmentId == query.DepartmentId.Value);

            if (query.ProvinceId.HasValue)
                q = q.Where(s => s.Distrit != null && s.Distrit.ProvinceId == query.ProvinceId.Value);

            if (query.DistrictId.HasValue)
                q = q.Where(s => s.DistritId == query.DistrictId.Value);

            if (!string.IsNullOrEmpty(query.Name))
            {
                var lowerName = query.Name.ToLower();
                q = q.Where(s => s.Name.ToLower().Contains(lowerName) || s.Code.Contains(query.Name));
            }

            // Ordenar sobre la entidad ANTES de proyectar — EF Core 10 no traduce
            // OrderBy sobre records posicionales (positional record constructor).
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "name" => query.IsDescending ? q.OrderByDescending(s => s.Name) : q.OrderBy(s => s.Name),
                "code" => query.IsDescending ? q.OrderByDescending(s => s.Code) : q.OrderBy(s => s.Code),
                "level" => query.IsDescending ? q.OrderByDescending(s => s.Level) : q.OrderBy(s => s.Level),
                "distrit.name" => query.IsDescending ? q.OrderByDescending(s => s.Distrit!.Name) : q.OrderBy(s => s.Distrit!.Name),
                _ => q.OrderBy(s => s.Name)
            };

            var projected = q.Select(s => new SchoolListItem(
                s.Id,
                s.Name,
                s.Code,
                s.UgelName,
                s.Modality,
                s.Level,
                s.Management,
                s.Address,
                s.Distrit != null ? s.Distrit.Name : null,
                s.Distrit != null && s.Distrit.Province != null ? s.Distrit.Province.Name : null,
                s.Distrit != null && s.Distrit.Province != null && s.Distrit.Province.Department != null
                    ? s.Distrit.Province.Department.Name
                    : null));

            return await PagedResult<SchoolListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public async Task<Schools> CreateAsync(Schools school, string actor, CancellationToken ct = default)
        {
            school.Id = Guid.NewGuid();
            school.CreatedAt = DateTimeOffset.UtcNow;
            school.CreatedBy = actor;
            _context.Schools.Add(school);
            await _context.SaveChangesAsync(ct);
            return school;
        }

        public async Task<SchoolImportResult> ImportFromExcelAsync(Stream excelStream, string actor, CancellationToken ct = default)
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            var departments = await _context.Departments.AsNoTracking().ToListAsync(ct);
            var provinces = await _context.Provincies.AsNoTracking().ToListAsync(ct);
            var districts = await _context.Distrits.AsNoTracking().ToListAsync(ct);

            var newSchools = new List<Schools>();
            var errors = new List<SchoolImportError>();

            foreach (var row in rows)
            {
                var cellCount = row.Cells().Count();
                var importRow = new SchoolImportRow(
                    Region: row.Cell(1).GetValue<string>().Trim().ToUpper(),
                    Province: row.Cell(2).GetValue<string>().Trim().ToUpper(),
                    District: row.Cell(3).GetValue<string>().Trim().ToUpper(),
                    Ugel: row.Cell(4).GetValue<string>().Trim(),
                    Code: row.Cell(5).GetValue<string>().Trim(),
                    Name: row.Cell(6).GetValue<string>().Trim(),
                    Modality: row.Cell(7).GetValue<string>().Trim(),
                    Level: row.Cell(8).GetValue<string>().Trim(),
                    Management: cellCount >= 9 ? row.Cell(9).GetValue<string>().Trim().ToUpperInvariant() : "PÚBLICO");

                if (string.IsNullOrEmpty(importRow.Name)) continue;

                string? rowError = null;
                var dept = departments.FirstOrDefault(d => d.Name.ToUpper() == importRow.Region);
                if (dept == null)
                {
                    rowError = $"Región '{importRow.Region}' no encontrada.";
                }
                else
                {
                    var prov = provinces.FirstOrDefault(p => p.Name.ToUpper() == importRow.Province && p.DepartmentId == dept.Id);
                    if (prov == null)
                    {
                        rowError = $"Provincia '{importRow.Province}' en '{importRow.Region}' no encontrada.";
                    }
                    else
                    {
                        var dist = districts.FirstOrDefault(d => d.Name.ToUpper() == importRow.District && d.ProvinceId == prov.Id);
                        if (dist == null)
                        {
                            rowError = $"Distrito '{importRow.District}' en '{importRow.Province}' no encontrado.";
                        }
                        else
                        {
                            newSchools.Add(new Schools
                            {
                                Id = Guid.NewGuid(),
                                Name = importRow.Name,
                                Code = importRow.Code,
                                UgelName = importRow.Ugel,
                                Modality = importRow.Modality,
                                Level = importRow.Level,
                                Management = importRow.Management,
                                DistritId = dist.Id,
                                CreatedBy = actor,
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                        }
                    }
                }

                if (rowError != null)
                {
                    errors.Add(new SchoolImportError(importRow, rowError));
                }
            }

            if (newSchools.Any())
            {
                _context.Schools.AddRange(newSchools);
                await _context.SaveChangesAsync(ct);
            }

            return new SchoolImportResult
            {
                ImportedCount = newSchools.Count,
                Errors = errors
            };
        }
    }
}
