namespace ADMISION.Models.ViewModels.Admin
{
    /// <summary>
    /// Modelo del formulario de creación/edición de un perfil de calificación.
    /// Los rangos se publican desde filas dinámicas del formulario (Ranges[i].*).
    /// </summary>
    public class ScoringProfileFormModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsWeighted { get; set; }
        public decimal PuntosCorrecta { get; set; }
        public decimal PuntosBlanco { get; set; }
        public decimal PuntosIncorrecta { get; set; }
        public decimal NotaMinimaIngreso { get; set; }
        public bool AplicarVigesimal { get; set; }
        public string ManejoAnuladas { get; set; } = "Ignorar";
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? CareerId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<ScoringProfileRangeFormModel> Ranges { get; set; } = new();
    }

    public class ScoringProfileRangeFormModel
    {
        public int FromQuestion { get; set; }
        public int ToQuestion { get; set; }
        public decimal PuntosCorrecta { get; set; }
    }
}
