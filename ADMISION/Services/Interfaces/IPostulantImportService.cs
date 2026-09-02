using ADMISION.Models.ViewModels.Admin;

namespace ADMISION.Services.Interfaces;

public interface IPostulantImportService
{
    Task<List<PostulantImportRow>> PreviewAsync(Stream excelStream, CancellationToken ct = default);
    byte[] BuildPostulantsTemplate();
    Task<PostulantImportResult> ExecuteImportAsync(Stream excelStream, string actor, CancellationToken ct = default);
    Task ImportBackgroundAsync(Guid jobId, string tempPath, string actor, Func<ImportProgress, Task>? onProgress = null, CancellationToken ct = default);
}

public class PostulantImportResult
{
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public List<PostulantImportRow> FailedRows { get; set; } = new();
}

public class ImportProgress
{
    public int Processed { get; set; }
    public int Total { get; set; }
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public double Percent => Total > 0 ? Math.Round((double)Processed / Total * 100, 1) : 0;
}
