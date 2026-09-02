using System.Text;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Ubigeo;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class UbigeoService : IUbigeoService
    {
        private readonly AppDbContext _context;

        public UbigeoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<UbigeoOption>> GetCountriesAsync(CancellationToken ct = default)
        {
            return await _context.Countries
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new UbigeoOption(c.Id, c.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<UbigeoOption>> GetAllDepartmentsAsync(CancellationToken ct = default)
        {
            return await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .Select(d => new UbigeoOption(d.Id, d.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<UbigeoOption>> GetDepartmentsAsync(Guid countryId, CancellationToken ct = default)
        {
            return await _context.Departments
                .AsNoTracking()
                .Where(d => d.CountryId == countryId)
                .OrderBy(d => d.Name)
                .Select(d => new UbigeoOption(d.Id, d.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<UbigeoOption>> GetProvincesAsync(Guid departmentId, CancellationToken ct = default)
        {
            return await _context.Provincies
                .AsNoTracking()
                .Where(p => p.DepartmentId == departmentId)
                .OrderBy(p => p.Name)
                .Select(p => new UbigeoOption(p.Id, p.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<UbigeoOption>> GetDistrictsAsync(Guid provinceId, CancellationToken ct = default)
        {
            return await _context.Distrits
                .AsNoTracking()
                .Where(d => d.ProvinceId == provinceId)
                .OrderBy(d => d.Name)
                .Select(d => new UbigeoOption(d.Id, d.Name))
                .ToListAsync(ct);
        }

        public async Task<UbigeoLookupResult?> FindByCodeAsync(string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            code = code.Trim();
            if (code.Length != 6 || !code.All(char.IsDigit)) return null;

            var hit = await _context.Distrits
                .AsNoTracking()
                .Where(d => d.Code == code)
                .Select(d => new
                {
                    d.Id,
                    DistritName = d.Name,
                    ProvinceId = d.Province!.Id,
                    ProvinceName = d.Province!.Name,
                    DepartmentId = d.Province!.Department!.Id,
                    DepartmentName = d.Province!.Department!.Name
                })
                .FirstOrDefaultAsync(ct);

            return hit == null
                ? null
                : new UbigeoLookupResult(hit.Id, hit.DistritName, hit.ProvinceId, hit.ProvinceName, hit.DepartmentId, hit.DepartmentName);
        }

        public async Task<UbigeoCounts> GetCountsAsync(CancellationToken ct = default)
        {
            var deps = await _context.Departments.CountAsync(ct);
            var provs = await _context.Provincies.CountAsync(ct);
            var dists = await _context.Distrits.CountAsync(ct);
            return new UbigeoCounts(deps, provs, dists);
        }

        public async Task<List<DepartmentWithProvincesDto>> GetFullUbigeoDataAsync(Guid countryId, CancellationToken ct = default)
        {
            var departments = await _context.Departments
                .AsNoTracking()
                .Where(d => d.CountryId == countryId)
                .OrderBy(d => d.Name)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Code
                })
                .ToListAsync(ct);

            var deptIds = departments.Select(d => d.Id).ToHashSet();

            var provinces = await _context.Provincies
                .AsNoTracking()
                .Where(p => deptIds.Contains(p.DepartmentId))
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Code,
                    p.DepartmentId
                })
                .ToListAsync(ct);

            var provIds = provinces.Select(p => p.Id).ToHashSet();

            var districts = await _context.Distrits
                .AsNoTracking()
                .Where(d => provIds.Contains(d.ProvinceId))
                .OrderBy(d => d.Name)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Code,
                    d.ProvinceId
                })
                .ToListAsync(ct);

            return departments.Select(d => new DepartmentWithProvincesDto(
                d.Id,
                d.Name,
                d.Code,
                provinces
                    .Where(p => p.DepartmentId == d.Id)
                    .Select(p => new ProvinceWithDistrictsDto(
                        p.Id,
                        p.Name,
                        p.Code,
                        districts
                            .Where(dd => dd.ProvinceId == p.Id)
                            .Select(dd => new DistrictSimpleDto(dd.Id, dd.Name, dd.Code))
                            .ToList()
                    ))
                    .ToList()
            )).ToList();
        }

        public async Task<Department> CreateDepartmentAsync(string name, string code, Guid countryId, string actor, CancellationToken ct = default)
        {
            var dept = new Department
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Code = code.Trim(),
                CountryId = countryId,
                CreatedBy = actor
            };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync(ct);
            return dept;
        }

        public async Task<Department> UpdateDepartmentAsync(Guid id, string name, string code, string? actor, CancellationToken ct = default)
        {
            var dept = await _context.Departments.FirstAsync(d => d.Id == id, ct);
            dept.Name = name.Trim();
            dept.Code = code.Trim();
            dept.UpdatedAt = DateTimeOffset.UtcNow;
            dept.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);
            return dept;
        }

        public async Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default)
        {
            var dept = await _context.Departments
                .Include(d => d.Provincies)
                .ThenInclude(p => p.Distrits)
                .FirstAsync(d => d.Id == id, ct);

            foreach (var prov in dept.Provincies ?? new List<Provincie>())
            {
                if (prov.Distrits != null)
                    _context.Distrits.RemoveRange(prov.Distrits);
            }
            if (dept.Provincies != null)
                _context.Provincies.RemoveRange(dept.Provincies);
            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<Provincie> CreateProvinceAsync(string name, string code, Guid departmentId, string actor, CancellationToken ct = default)
        {
            var prov = new Provincie
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Code = code.Trim(),
                DepartmentId = departmentId,
                CreatedBy = actor
            };
            _context.Provincies.Add(prov);
            await _context.SaveChangesAsync(ct);
            return prov;
        }

        public async Task<Provincie> UpdateProvinceAsync(Guid id, string name, string code, string? actor, CancellationToken ct = default)
        {
            var prov = await _context.Provincies.FirstAsync(p => p.Id == id, ct);
            prov.Name = name.Trim();
            prov.Code = code.Trim();
            prov.UpdatedAt = DateTimeOffset.UtcNow;
            prov.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);
            return prov;
        }

        public async Task DeleteProvinceAsync(Guid id, CancellationToken ct = default)
        {
            var prov = await _context.Provincies
                .Include(p => p.Distrits)
                .FirstAsync(p => p.Id == id, ct);

            if (prov.Distrits != null)
                _context.Distrits.RemoveRange(prov.Distrits);
            _context.Provincies.Remove(prov);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<Distrit> CreateDistrictAsync(string name, string code, Guid provinceId, string actor, CancellationToken ct = default)
        {
            var dist = new Distrit
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Code = code.Trim(),
                ProvinceId = provinceId,
                CreatedBy = actor
            };
            _context.Distrits.Add(dist);
            await _context.SaveChangesAsync(ct);
            return dist;
        }

        public async Task<Distrit> UpdateDistrictAsync(Guid id, string name, string code, string? actor, CancellationToken ct = default)
        {
            var dist = await _context.Distrits.FirstAsync(d => d.Id == id, ct);
            dist.Name = name.Trim();
            dist.Code = code.Trim();
            dist.UpdatedAt = DateTimeOffset.UtcNow;
            dist.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);
            return dist;
        }

        public async Task DeleteDistrictAsync(Guid id, CancellationToken ct = default)
        {
            var dist = await _context.Distrits.FirstAsync(d => d.Id == id, ct);
            _context.Distrits.Remove(dist);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<UbigeoImportResult> ImportCsvAsync(Stream csvStream, Guid countryId, string actor, CancellationToken ct = default)
        {
            var encoding = DetectCsvEncoding(csvStream);
            using var reader = new StreamReader(csvStream, encoding, leaveOpen: true);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return new UbigeoImportResult(0, 0, 0);
            }

            var departments = await _context.Departments.AsNoTracking()
                .Where(d => d.CountryId == countryId)
                .ToDictionaryAsync(d => d.Code, ct);

            var provinces = await _context.Provincies.AsNoTracking()
                .Where(p => p.Department.CountryId == countryId)
                .ToDictionaryAsync(p => p.Code, ct);

            var districts = await _context.Distrits.AsNoTracking()
                .Where(d => d.Province.Department.CountryId == countryId)
                .ToDictionaryAsync(d => d.Code, ct);

            int dCount = 0, pCount = 0, distCount = 0;
            var separator = headerLine.Contains(';') ? ';' : ',';

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(separator);
                if (parts.Length < 4) continue;

                var ubigeoCode = parts[0].Trim().Replace("\"", "");
                var deptName = parts[1].Trim().Replace("\"", "");
                var provName = parts[2].Trim().Replace("\"", "");
                var distName = parts[3].Trim().Replace("\"", "");

                if (ubigeoCode.Length != 6) continue;

                var dCode = ubigeoCode[..2];
                var pCode = ubigeoCode[..4];

                if (!departments.TryGetValue(dCode, out var dept))
                {
                    dept = new Department
                    {
                        Id = Guid.NewGuid(),
                        Code = dCode,
                        Name = deptName,
                        CountryId = countryId,
                        CreatedBy = actor
                    };
                    _context.Departments.Add(dept);
                    departments.Add(dCode, dept);
                    dCount++;
                }

                if (!provinces.TryGetValue(pCode, out var prov))
                {
                    prov = new Provincie
                    {
                        Id = Guid.NewGuid(),
                        Code = pCode,
                        Name = provName,
                        DepartmentId = dept.Id,
                        CreatedBy = actor
                    };
                    _context.Provincies.Add(prov);
                    provinces.Add(pCode, prov);
                    pCount++;
                }

                if (!districts.ContainsKey(ubigeoCode))
                {
                    var dist = new Distrit
                    {
                        Id = Guid.NewGuid(),
                        Code = ubigeoCode,
                        Name = distName,
                        ProvinceId = prov.Id,
                        CreatedBy = actor
                    };
                    _context.Distrits.Add(dist);
                    districts.Add(ubigeoCode, dist);
                    distCount++;
                }
            }

            await _context.SaveChangesAsync(ct);
            return new UbigeoImportResult(dCount, pCount, distCount);
        }

        private static Encoding DetectCsvEncoding(Stream stream)
        {
            var bom = new byte[4];
            var pos = stream.Position;
            var read = stream.Read(bom, 0, 4);
            stream.Seek(pos, SeekOrigin.Begin);

            if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return Encoding.UTF8;

            if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
                return Encoding.Unicode;

            return Encoding.Latin1;
        }
    }
}
