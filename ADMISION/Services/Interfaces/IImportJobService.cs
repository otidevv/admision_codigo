using ADMISION.ENTITIES.Models.System;

namespace ADMISION.Services.Interfaces;

public interface IImportJobService
{
    Task<ImportJob> CreateAsync(string fileName, int totalRows, string tempToken, string createdBy);
    Task UpdateProgressAsync(Guid jobId, int processed, int total, int inserted, int skipped, int failed);
    Task CompleteAsync(Guid jobId, int inserted, int skipped, int failed);
    Task FailAsync(Guid jobId, string errorMessage);
    Task<ImportJob?> GetByIdAsync(Guid jobId);
}
