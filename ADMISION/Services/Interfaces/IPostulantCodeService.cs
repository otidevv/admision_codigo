namespace ADMISION.Services.Interfaces
{
    public interface IPostulantCodeService
    {
        /// <summary>
        /// Genera el próximo código de postulante para una modalidad.
        /// Si la modalidad tiene StartingCode configurado:
        ///   - Si ya hay inscritos con códigos numéricos, devuelve (max + 1) padded al largo de StartingCode.
        ///   - Si no hay inscritos, devuelve StartingCode tal cual.
        /// Si no hay StartingCode configurado, devuelve un código fallback basado en el documento.
        /// </summary>
        Task<string> GenerateNextAsync(Guid modalityId, string fallbackDocumentNumber);
    }
}
