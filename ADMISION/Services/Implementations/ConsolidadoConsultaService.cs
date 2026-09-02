using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using admision.Models.ViewModels.Api;

namespace ADMISION.Services.Implementations;

public class ConsolidadoConsultaService : IConsolidadoConsultaService
{
    private readonly AppDbContext _context;

    public ConsolidadoConsultaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ConsolidadoIngresantesVersion?> GetLatestVersionAsync(CancellationToken ct = default)
    {
        var activeTerm = await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive, ct);

        if (activeTerm == null) return null;

        return await _context.ConsolidadoIngresantesVersions
            .AsNoTracking()
            .Where(v => v.TermId == activeTerm.Id && v.IsLatest)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ConsolidadoIngresantesVersion?> GetLatestVersionByTermAsync(Guid termId, CancellationToken ct = default)
    {
        return await _context.ConsolidadoIngresantesVersions
            .AsNoTracking()
            .Where(v => v.TermId == termId && v.IsLatest)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<ConsolidadoIngresantesRecordDto>> GetRecordsByVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        return await _context.ConsolidadoIngresantesRecords
            .AsNoTracking()
            .Where(r => r.VersionId == versionId)
            .OrderBy(r => r.Nro)
                .Select(r => new ConsolidadoIngresantesRecordDto
                {
                    CodigoEstudiante = r.CodigoEstudiante,
                    CodigoCarrera = r.CodigoCarrera,
                    SegundaCarrera = r.SegundaCarrera,
                    Semestre = r.Semestre,
                    Nombres = r.Nombres,
                    Paterno = r.Paterno,
                    Materno = r.Materno,
                    DType = r.DType,
                    Dni = r.DNI,
                    Email = r.Email,
                    Celular = r.Celular,
                    Direccion = r.Direccion,
                    FechaNacimiento = r.FechaNacimiento,
                    Sexo = r.Sexo,
                    EstadoCivil = r.EstadoCivil,
                    Ubigeo = r.Ubigeo,
                    TipoPostulante = r.TipoPostulante,
                    TipoObs = r.TipoObs,
                    Observaciones = r.Observaciones,
                    Nro = r.Nro
                })
            .ToListAsync(ct);
    }
}