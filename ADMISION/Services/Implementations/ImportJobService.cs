using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.System;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations;

public class ImportJobService : IImportJobService
{
    private readonly AppDbContext _context;

    public ImportJobService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ImportJob> CreateAsync(string fileName, int totalRows, string tempToken, string createdBy)
    {
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            TotalRows = totalRows,
            TempToken = tempToken,
            CreatedBy = createdBy,
            Status = "Pending"
        };
        _context.ImportJobs.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    public async Task UpdateProgressAsync(Guid jobId, int processed, int total, int inserted, int skipped, int failed)
    {
        var job = await _context.ImportJobs.FindAsync(jobId);
        if (job == null) return;
        job.ProcessedRows = processed;
        job.TotalRows = total;
        job.Inserted = inserted;
        job.Skipped = skipped;
        job.FailedRows = failed;
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task CompleteAsync(Guid jobId, int inserted, int skipped, int failed)
    {
        var job = await _context.ImportJobs.FindAsync(jobId);
        if (job == null) return;
        job.Status = "Completed";
        job.Inserted = inserted;
        job.Skipped = skipped;
        job.FailedRows = failed;
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task FailAsync(Guid jobId, string errorMessage)
    {
        var job = await _context.ImportJobs.FindAsync(jobId);
        if (job == null) return;
        job.Status = "Failed";
        job.ErrorMessage = errorMessage;
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<ImportJob?> GetByIdAsync(Guid jobId)
    {
        return await _context.ImportJobs.FindAsync(jobId);
    }
}
