using ADMISION.Services.Interfaces;
using Hangfire;

namespace ADMISION.Services.Background;

public class PostulantImportJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostulantImportJob> _logger;

    public PostulantImportJob(IServiceScopeFactory scopeFactory, ILogger<PostulantImportJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(Guid jobId, string tempPath, string actor)
    {
        using var scope = _scopeFactory.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IPostulantImportService>();
        var jobService = scope.ServiceProvider.GetRequiredService<IImportJobService>();

        try
        {
            var job = await jobService.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("ImportJob {JobId} not found", jobId);
                return;
            }

            _logger.LogInformation("Starting import job {JobId} — {File}", jobId, job.FileName);
            await jobService.UpdateProgressAsync(jobId, 0, job.TotalRows, 0, 0, 0);

            await importService.ImportBackgroundAsync(jobId, tempPath, actor, async progress =>
            {
                await jobService.UpdateProgressAsync(jobId, progress.Processed, progress.Total, progress.Inserted, progress.Skipped, progress.Failed);
            });

            var final = await jobService.GetByIdAsync(jobId);
            await jobService.CompleteAsync(jobId, final?.Inserted ?? 0, final?.Skipped ?? 0, final?.FailedRows ?? 0);

            _logger.LogInformation("Import job {JobId} completed: {Inserted} inserted, {Skipped} skipped, {Failed} failed",
                jobId, final?.Inserted ?? 0, final?.Skipped ?? 0, final?.FailedRows ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import job {JobId} failed", jobId);
            try { await jobService.FailAsync(jobId, ex.Message); } catch { }
        }
    }
}
