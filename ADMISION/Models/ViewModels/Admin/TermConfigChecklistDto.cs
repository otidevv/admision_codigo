namespace ADMISION.Models.ViewModels.Admin
{
    /// <summary>
    /// Estado de configuración de un periodo académico. Cada item del checklist
    /// representa un requisito que debe estar listo para poder habilitar la
    /// inscripción de postulantes. Lo consume la vista <c>Admin/Terms/Index</c>.
    /// </summary>
    public class TermConfigChecklistDto
    {
        public Guid TermId { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string TermYear { get; set; } = string.Empty;
        public bool TermIsActive { get; set; }

        public List<TermConfigChecklistItem> Items { get; set; } = new();

        public int Total => Items.Count;
        public int DoneCount => Items.Count(i => i.Done);
        public int PendingCount => Total - DoneCount;
        public int PercentComplete => Total == 0 ? 0 : (int)Math.Round(DoneCount * 100.0 / Total);
        public bool IsFullyConfigured => Total > 0 && DoneCount == Total;
    }

    /// <summary>
    /// Cada item del checklist. <see cref="Done"/> marca si está listo;
    /// <see cref="Count"/> da contexto cuantitativo (p. ej. "12 carreras con
    /// vacantes"); <see cref="Href"/> apunta a la pantalla de configuración.
    /// </summary>
    public class TermConfigChecklistItem
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-circle-check";
        public bool Done { get; set; }
        public int Count { get; set; }
        /// <summary>"primary" | "danger" | "warn" — para colorear items críticos pendientes.</summary>
        public string Severity { get; set; } = "primary";
        public string? Href { get; set; }
        /// <summary>Mensaje específico cuando falta (p. ej. "2 modalidades sin requisitos").</summary>
        public string? PendingDetail { get; set; }
    }
}
