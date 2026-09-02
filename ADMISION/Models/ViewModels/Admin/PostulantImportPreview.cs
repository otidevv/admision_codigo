namespace ADMISION.Models.ViewModels.Admin;

public class PostulantImportPreview
{
    public List<PostulantImportRow> Rows { get; set; } = new();
    public string FileName { get; set; } = "";
    public string TempToken { get; set; } = "";
    public Guid JobId { get; set; }
}
