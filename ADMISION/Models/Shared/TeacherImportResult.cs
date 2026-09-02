namespace ADMISION.Models.Shared
{
    public record TeacherImportRow(
        string DNI,
        string ApellidoPaterno,
        string ApellidoMaterno,
        string Nombres,
        string Especialidad,
        string Grado,
        string Tipo);

    public record TeacherImportError(TeacherImportRow Row, string Error);

    public class TeacherImportResult
    {
        public int ImportedCount { get; init; }
        public IReadOnlyList<TeacherImportError> Errors { get; init; } = Array.Empty<TeacherImportError>();
    }
}
