using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class CepreReportService : ICepreReportService
    {
        private readonly AppDbContext _context;

        public CepreReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CepreImportVersion>> GetVersionsAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.CepreImportVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(ct);
        }

        public async Task<CepreReportViewModel> BuildAsync(CepreReportFilter filter, CancellationToken ct = default)
        {
            var vm = await BuildContextAsync(filter, ct);
            if (!vm.VersionId.HasValue) return vm;

            var query = _context.CepreImportRecords.AsNoTracking()
                .Where(r => r.VersionId == vm.VersionId.Value);

            vm.TotalRecords = await query.CountAsync(ct);
            vm.PageSize = Math.Max(1, filter.PageSize);
            vm.Page = Math.Clamp(filter.Page, 1, Math.Max(1, (int)Math.Ceiling((double)vm.TotalRecords / vm.PageSize)));

            var records = await query
                .OrderBy(r => r.Nro)
                .Skip((vm.Page - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .Select(r => new CepreReportItem
                {
                    Nro = r.Nro,
                    Ciclo = r.Ciclo ?? string.Empty,
                    Codigo = r.Codigo ?? string.Empty,
                    Dni = r.Dni ?? string.Empty,
                    TDocumento = r.TDocumento ?? string.Empty,
                    Apaterno = r.Apaterno ?? string.Empty,
                    Amaterno = r.Amaterno ?? string.Empty,
                    Nombres = r.Nombres ?? string.Empty,
                    ApellidosNombres = r.ApellidosNombres ?? string.Empty,
                    Sexo = r.Sexo ?? string.Empty,
                    FechaNacimiento = r.FechaNacimiento ?? string.Empty,
                    Direccion = r.Direccion ?? string.Empty,
                    EstadoCivil = r.EstadoCivil ?? string.Empty,
                    AnioEgreso = r.AnioEgreso ?? string.Empty,
                    Correo = r.Correo ?? string.Empty,
                    Celular = r.Celular ?? string.Empty,
                    Colegio = r.Colegio ?? string.Empty,
                    NombreColegio = r.NombreColegio ?? string.Empty,
                    UbigeoColegio = r.UbigeoColegio ?? string.Empty,
                    DireccionColegio = r.DireccionColegio ?? string.Empty,
                    Ubigeo = r.Ubigeo ?? string.Empty,
                    Departamento = r.Departamento ?? string.Empty,
                    Provincia = r.Provincia ?? string.Empty,
                    Distrito = r.Distrito ?? string.Empty,
                    LugarNacimiento = r.LugarNacimiento ?? string.Empty,
                    Modalidad = r.Modalidad ?? string.Empty,
                    CodigoCarrera = r.CodigoCarrera ?? string.Empty,
                    CarreraProfesional = r.CarreraProfesional ?? string.Empty,
                    Grupo = r.Grupo ?? string.Empty,
                    ModalidadPago = r.ModalidadPago ?? string.Empty,
                    Monto = r.Monto,
                    Puntaje01 = r.Puntaje01,
                    Nota01 = r.Nota01,
                    Puntaje02 = r.Puntaje02,
                    Nota02 = r.Nota02,
                    Puntaje03 = r.Puntaje03,
                    Nota03 = r.Nota03,
                    NotaFinal = r.NotaFinal,
                    Puntaje = r.Puntaje,
                    Estado = r.Estado ?? string.Empty
                })
                .ToListAsync(ct);

            vm.Items = records;
            return vm;
        }

        public async Task<CepreReportViewModel> BuildAllAsync(CepreReportFilter filter, CancellationToken ct = default)
        {
            var vm = await BuildContextAsync(filter, ct);
            if (!vm.VersionId.HasValue) return vm;

            var records = await _context.CepreImportRecords.AsNoTracking()
                .Where(r => r.VersionId == vm.VersionId.Value)
                .OrderBy(r => r.Nro)
                .Select(r => new CepreReportItem
                {
                    Nro = r.Nro,
                    Ciclo = r.Ciclo ?? string.Empty,
                    Codigo = r.Codigo ?? string.Empty,
                    Dni = r.Dni ?? string.Empty,
                    TDocumento = r.TDocumento ?? string.Empty,
                    Apaterno = r.Apaterno ?? string.Empty,
                    Amaterno = r.Amaterno ?? string.Empty,
                    Nombres = r.Nombres ?? string.Empty,
                    ApellidosNombres = r.ApellidosNombres ?? string.Empty,
                    Sexo = r.Sexo ?? string.Empty,
                    FechaNacimiento = r.FechaNacimiento ?? string.Empty,
                    Direccion = r.Direccion ?? string.Empty,
                    EstadoCivil = r.EstadoCivil ?? string.Empty,
                    AnioEgreso = r.AnioEgreso ?? string.Empty,
                    Correo = r.Correo ?? string.Empty,
                    Celular = r.Celular ?? string.Empty,
                    Colegio = r.Colegio ?? string.Empty,
                    NombreColegio = r.NombreColegio ?? string.Empty,
                    UbigeoColegio = r.UbigeoColegio ?? string.Empty,
                    DireccionColegio = r.DireccionColegio ?? string.Empty,
                    Ubigeo = r.Ubigeo ?? string.Empty,
                    Departamento = r.Departamento ?? string.Empty,
                    Provincia = r.Provincia ?? string.Empty,
                    Distrito = r.Distrito ?? string.Empty,
                    LugarNacimiento = r.LugarNacimiento ?? string.Empty,
                    Modalidad = r.Modalidad ?? string.Empty,
                    CodigoCarrera = r.CodigoCarrera ?? string.Empty,
                    CarreraProfesional = r.CarreraProfesional ?? string.Empty,
                    Grupo = r.Grupo ?? string.Empty,
                    ModalidadPago = r.ModalidadPago ?? string.Empty,
                    Monto = r.Monto,
                    Puntaje01 = r.Puntaje01,
                    Nota01 = r.Nota01,
                    Puntaje02 = r.Puntaje02,
                    Nota02 = r.Nota02,
                    Puntaje03 = r.Puntaje03,
                    Nota03 = r.Nota03,
                    NotaFinal = r.NotaFinal,
                    Puntaje = r.Puntaje,
                    Estado = r.Estado ?? string.Empty
                })
                .ToListAsync(ct);

            vm.Items = records;
            vm.TotalRecords = records.Count;
            return vm;
        }

        private async Task<CepreReportViewModel> BuildContextAsync(CepreReportFilter filter, CancellationToken ct)
        {
            var vm = new CepreReportViewModel { TermId = filter.TermId };

            if (!filter.TermId.HasValue) return vm;

            var termId = filter.TermId.Value;
            var term = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == termId, ct);

            if (term == null) return vm;

            vm.TermName = term.Name;

            // Versión seleccionada o, por defecto, la última activa del período.
            CepreImportVersion? version = null;
            if (filter.VersionId.HasValue)
            {
                version = await _context.CepreImportVersions.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == filter.VersionId.Value && v.TermId == termId, ct);
            }

            version ??= await _context.CepreImportVersions.AsNoTracking()
                .Where(v => v.TermId == termId && v.IsLatest)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(ct);

            if (version == null) return vm;

            vm.VersionId = version.Id;
            vm.VersionLabel = $"Versión {version.VersionNumber}";
            return vm;
        }
    }
}
