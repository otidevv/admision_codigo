using ADMISION.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Extensions
{
    /// <summary>
    /// Helpers para que cualquier controller pueda capturar excepciones de
    /// guardado de archivos / DB y exponerlas al navegador de forma uniforme:
    ///
    ///  • TempData["SwalError"]       → mensaje corto que el _AdminLayout muestra
    ///                                   con un SweetAlert.
    ///  • TempData["SwalErrorDetail"] → detalle completo (tipo + mensaje + stack)
    ///                                   que el layout vuelca a console.error
    ///                                   (modo prueba; luego se puede silenciar).
    ///  • ModelState                  → además se agrega como error de validación
    ///                                   por si la vista lo muestra inline.
    /// </summary>
    public static class ControllerErrorExtensions
    {
        public static void SetSaveError(this Controller controller, Exception ex, ILogger? logger = null)
        {
            var userMessage = BuildUserMessage(ex);
            var detail = BuildDetail(ex);

            controller.TempData["SwalError"] = userMessage;
            controller.TempData["SwalErrorDetail"] = detail;
            controller.ModelState.AddModelError(string.Empty, userMessage);

            logger?.LogError(ex, "Error al guardar en {Controller}", controller.GetType().Name);
        }

        private static string BuildUserMessage(Exception ex) => ex switch
        {
            InvalidFileException ife =>
                $"{ife.FileName}: {ife.Reason}",
            UnauthorizedAccessException =>
                "El servidor no tiene permisos para escribir en la carpeta de uploads. " +
                "Verifica los permisos del App Pool de IIS sobre BaseStoragePath.",
            DirectoryNotFoundException dnf =>
                $"La carpeta de destino no existe: {dnf.Message}",
            PathTooLongException =>
                "La ruta del archivo es demasiado larga para el sistema de archivos.",
            IOException io =>
                $"Error de E/S al guardar el archivo: {io.Message}",
            DbUpdateException du =>
                "Error al guardar en base de datos: " + (du.InnerException?.Message ?? du.Message),
            _ =>
                $"Error inesperado: {ex.Message}"
        };

        private static string BuildDetail(Exception ex)
        {
            var lines = new List<string>
            {
                $"[{ex.GetType().FullName}]",
                ex.Message,
                string.Empty,
                "Stack:",
                ex.StackTrace ?? "(sin stack)"
            };

            var inner = ex.InnerException;
            int depth = 1;
            while (inner != null && depth <= 5)
            {
                lines.Add(string.Empty);
                lines.Add($"--- Inner #{depth} [{inner.GetType().FullName}] ---");
                lines.Add(inner.Message);
                if (inner.StackTrace != null)
                {
                    lines.Add(inner.StackTrace);
                }
                inner = inner.InnerException;
                depth++;
            }

            return string.Join("\n", lines);
        }
    }
}
