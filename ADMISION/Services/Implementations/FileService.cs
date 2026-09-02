using ADMISION.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ADMISION.Services.Implementations
{
    /// <summary>
    /// Resultado detallado de validación de archivo.
    /// </summary>
    public record FileValidationResult(bool IsValid, string Reason = "");

    /// <summary>
    /// Excepción lanzada por <see cref="FileService.SaveFileAsync"/> cuando un archivo no pasa
    /// las validaciones. Incluye el nombre original del archivo para que las capas superiores
    /// puedan identificar cuál rechazó el sistema.
    /// </summary>
    public class InvalidFileException : InvalidOperationException
    {
        public string FileName { get; }
        public string Reason { get; }

        public InvalidFileException(string fileName, string reason)
            : base($"Archivo no válido: {fileName} — {reason}")
        {
            FileName = fileName;
            Reason = reason;
        }
    }

    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;

        // Extensiones que mapean MIME a su extensión canónica (para validación cruzada).
        private static readonly Dictionary<string, string[]> _mimeToExtensions = new()
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/pjpeg"] = [".jpg", ".jpeg"],
            ["image/jpg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/x-png"] = [".png"],
            ["image/gif"] = [".gif"],
            ["image/webp"] = [".webp"],
            ["image/x-webp"] = [".webp"],
            ["image/bmp"] = [".bmp"],
            ["image/x-icon"] = [".ico"],
            ["application/pdf"] = [".pdf"],
            ["application/x-pdf"] = [".pdf"],
            ["image/heic"] = [".heic"],
            ["image/heif"] = [".heif"],
            ["image/heic-sequence"] = [".heic"],
            ["image/heif-sequence"] = [".heif"],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [".docx"],
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = [".xlsx"],
            ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = [".pptx"],
            ["application/msword"] = [".doc"],
            ["application/vnd.ms-excel"] = [".xls"],
            ["application/zip"] = [".zip"],
            ["application/x-zip-compressed"] = [".zip"],
            ["application/rar"] = [".rar"],
            ["application/x-rar-compressed"] = [".rar"],
            ["application/octet-stream"] = [".bin", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".ico", ".pdf", ".heic", ".heif", ".docx", ".xlsx", ".pptx", ".doc", ".xls", ".zip", ".rar"],
            ["text/plain"] = [".txt"],
            ["text/csv"] = [".csv"],
            ["text/xml"] = [".xml"],
            ["application/json"] = [".json"],
        };

        // Extensiones que NUNCA deben aparecer como segmento intermedio del nombre
        // (ataque de doble extensión: "malware.exe.pdf", "shell.php.jpg").
        private static readonly HashSet<string> _dangerousInnerExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "exe", "dll", "bat", "cmd", "com", "msi", "scr", "pif",
            "ps1", "psm1", "vbs", "vbe", "js", "jse", "wsf", "wsh",
            "sh", "bash", "zsh", "php", "phtml", "asp", "aspx",
            "jsp", "jar", "py", "rb", "pl", "lnk", "hta",
        };

        public FileService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<FileService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═════════════════════════════════════════════════════════════════════

        public async Task<string> SaveFileAsync(IFormFile file, string module)
        {
            if (file == null || file.Length == 0) return string.Empty;

            var validation = await ValidateFileAsync(file);
            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Archivo rechazado [{FileName}] — módulo: {Module} — razón: {Reason}",
                    file.FileName, module, validation.Reason);
                throw new InvalidFileException(file.FileName ?? "archivo", validation.Reason);
            }

            var year = DateTime.Now.Year.ToString();
            var basePath = GetBaseStoragePath();
            var relativeFolder = Path.Combine(year, module);
            var absoluteFolder = Path.Combine(basePath, relativeFolder);

            if (!Directory.Exists(absoluteFolder))
                Directory.CreateDirectory(absoluteFolder);

            // Nombre limpio: solo GUID + extensión conocida (no usamos el nombre original)
            var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var relativePath = Path.Combine(relativeFolder, fileName);
            var absolutePath = Path.Combine(basePath, relativePath);

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation(
                "Archivo guardado: {RelativePath} ({Bytes} bytes)", relativePath, file.Length);

            // Prefijo "uploads/" para que encaje con el PhysicalFileProvider
            // registrado en Program.cs bajo RequestPath="/uploads".
            return "uploads/" + relativePath.Replace("\\", "/");
        }

        /// <summary>
        /// Validación: tamaño, nombre, extensión, MIME, y cross-check MIME↔extensión.
        /// </summary>
        public async Task<FileValidationResult> ValidateFileAsync(IFormFile file)
        {
            var config = _configuration.GetSection("FileUpload");
            var allowedExtensions = config.GetSection("AllowedExtensions").Get<string[]>() ?? [];
            var allowedMimeTypes = config.GetSection("AllowedMimeTypes").Get<string[]>() ?? [];
            var maxSizeInMB = config.GetValue<long>("MaxFileSizeInMB", 20);

            // 1 ── Tamaño ──────────────────────────────────────────────────────
            if (file.Length > maxSizeInMB * 1024 * 1024)
                return new(false, $"El archivo supera el límite de {maxSizeInMB} MB.");

            // 2 ── Nombre: null-byte injection / doble extensión ───────────────
            var fileName = file.FileName ?? string.Empty;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension))
                return new(false, "El archivo no tiene extensión.");

            if (fileName.Contains('\0'))
                return new(false, "El nombre del archivo contiene caracteres nulos.");

            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            foreach (var segment in nameWithoutExt.Split('.'))
            {
                if (_dangerousInnerExtensions.Contains(segment))
                    return new(false,
                        $"El nombre del archivo contiene una extensión ejecutable intermedia ('.{segment.ToLowerInvariant()}').");
            }

            // 3 ── Extensión en lista blanca ───────────────────────────────────
            if (!allowedExtensions.Contains(extension))
                return new(false, $"La extensión '{extension}' no está permitida.");

            // 4 ── Content-Type en lista blanca ────────────────────────────────
            var contentType = file.ContentType.ToLowerInvariant();
            if (!allowedMimeTypes.Contains(contentType))
                return new(false, $"El tipo MIME '{contentType}' no está permitido.");

            // 5 ── Cross-check MIME ↔ extensión ────────────────────────────────
            if (contentType != "application/octet-stream" && _mimeToExtensions.TryGetValue(contentType, out var expectedExtensions))
            {
                if (!expectedExtensions.Contains(extension))
                    return new(false,
                        $"El tipo MIME '{contentType}' no corresponde a la extensión '{extension}'.");
            }

            return new(true);
        }

        /// <summary>Compatibilidad: versión síncrona que llama la asíncrona. </summary>
        public bool IsFileSafe(IFormFile file)
        {
            var result = ValidateFileAsync(file).GetAwaiter().GetResult();
            return result.IsValid;
        }

        public void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            var absolutePath = GetAbsolutePath(relativePath);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
                _logger.LogInformation("Archivo eliminado: {RelativePath}", relativePath);
            }
        }

        public string GetAbsolutePath(string relativePath)
        {
            // Aceptar tanto "uploads/..." (nuevo formato) como "..." sin prefijo
            // (registros previos) para mantener compatibilidad hacia atrás.
            var cleaned = relativePath.Replace("\\", "/").TrimStart('/');
            if (cleaned.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring("uploads/".Length);
            return Path.Combine(GetBaseStoragePath(), cleaned);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Raíz física donde viven los archivos subidos. Contrato:
        /// lo que haya aquí se expone en la URL pública bajo "/uploads".
        ///
        /// Resolución:
        ///   • Si FileUpload:BaseStoragePath está configurado:
        ///       - Absoluto  → se usa tal cual.
        ///       - Relativo  → se resuelve contra ContentRoot del proyecto.
        ///   • Si no hay config → default a "{WebRoot}/uploads" (sirve solo por
        ///     UseStaticFiles por defecto, sin hace falta proveedor extra).
        /// </summary>
        public string GetBaseStoragePath()
        {
            var configured = _configuration["FileUpload:BaseStoragePath"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                if (Path.IsPathRooted(configured)) return configured;
                return Path.Combine(_environment.ContentRootPath, configured);
            }

            var webRoot = string.IsNullOrEmpty(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;

            return Path.Combine(webRoot, "uploads");
        }
    }
}