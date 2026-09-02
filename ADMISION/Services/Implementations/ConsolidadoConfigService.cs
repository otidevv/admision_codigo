using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations;

public class ConsolidadoConfigService : IConsolidadoConfigService
{
    private readonly AppDbContext _context;

    public ConsolidadoConfigService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Term>> GetTermsAsync()
    {
        return await _context.Terms
            .AsNoTracking()
            .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
            .ToListAsync();
    }

    public async Task<List<PostulantTypeConfig>> GetConfigurationsAsync(Guid termId)
    {
        return await _context.PostulantTypeConfigs
            .AsNoTracking()
            .Include(c => c.Career)
            .Include(c => c.Modality)
            .Include(c => c.TypeModality)
            .Where(c => c.TermId == termId)
            .OrderBy(c => c.Index)
            .ToListAsync();
    }

    public async Task<List<Career>> GetCareersAsync()
    {
        return await _context.Careers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Modality>> GetModalitiesAsync(Guid termId)
    {
        return await _context.Modalities
            .AsNoTracking()
            .Where(m => m.TermId == termId)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<List<TypeModality>> GetTypeModalitiesAsync(Guid termId)
    {
        var modalityIds = await _context.Modalities
            .AsNoTracking()
            .Where(m => m.TermId == termId)
            .Select(m => m.Id)
            .ToListAsync();

        return await _context.TypeModalities
            .AsNoTracking()
            .Where(tm => modalityIds.Contains(tm.ModalityId))
            .OrderBy(tm => tm.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsConfigurationAsync(Guid termId, int index)
    {
        return await _context.PostulantTypeConfigs
            .AnyAsync(c => c.TermId == termId && c.Index == index);
    }

    public async Task CreateConfigurationAsync(Guid termId, int index, string description, Guid? careerId, Guid? modalityId, Guid? typeModalityId, string createdBy)
    {
        var config = new PostulantTypeConfig
        {
            Id = Guid.NewGuid(),
            TermId = termId,
            Index = index,
            Description = description.Trim(),
            CareerId = careerId,
            ModalityId = modalityId,
            TypeModalityId = typeModalityId,
            CreatedBy = createdBy
        };

        _context.PostulantTypeConfigs.Add(config);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteConfigurationAsync(Guid id)
    {
        var config = await _context.PostulantTypeConfigs.FindAsync(id);
        if (config == null) return false;

        _context.PostulantTypeConfigs.Remove(config);
        await _context.SaveChangesAsync();
        return true;
    }
}
