namespace ADMISION.Services.Interfaces
{
    public interface IFileService
    {
        /// <summary>
        /// Saves a file to the storage path, organized by year and module.
        /// </summary>
        /// <param name="file">The file to save.</param>
        /// <param name="module">The module name (e.g., "Payments", "Requirements").</param>
        /// <returns>The relative path of the saved file.</returns>
        Task<string> SaveFileAsync(IFormFile file, string module);

        /// <summary>
        /// Full async validation — same checks que aplica SaveFileAsync (extensión,
        /// MIME, magic bytes, contenido malicioso). Útil para pre-validar todos los
        /// archivos de un formulario antes de tocar BD/disco, de modo que un archivo
        /// inválido al final del lote no deje filas a medias o archivos huérfanos.
        /// </summary>
        Task<ADMISION.Services.Implementations.FileValidationResult> ValidateFileAsync(IFormFile file);

        /// <summary>
        /// Validates if a file is safe (extension, mime type, size).
        /// </summary>
        /// <param name="file">The file to validate.</param>
        /// <returns>True if safe, false otherwise.</returns>
        bool IsFileSafe(IFormFile file);

        /// <summary>
        /// Deletes a file from storage.
        /// </summary>
        /// <param name="relativePath">The relative path of the file.</param>
        void DeleteFile(string relativePath);
        
        /// <summary>
        /// Gets the absolute path for a relative path based on environment.
        /// </summary>
        string GetAbsolutePath(string relativePath);

        /// <summary>
        /// Gets the base storage path (FileUpload:BaseStoragePath from config,
        /// or fallback to WebRootPath/uploads). All uploaded files should be
        /// stored under this root so they are served via the /uploads static
        /// file provider.
        /// </summary>
        string GetBaseStoragePath();
    }
}
