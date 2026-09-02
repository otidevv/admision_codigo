namespace ADMISION.Services.Interfaces
{
    public class ExternalScoringParameters
    {
        public decimal PuntosCorrecta { get; set; } = 5m;
        public decimal PuntosBlanco { get; set; } = 0.5m;
        public decimal PuntosIncorrecta { get; set; } = 0m;
        public decimal NotaMinimaIngreso { get; set; } = 0m;
        public bool AplicarVigesimal { get; set; } = false;
        public string ManejoAnuladas { get; set; } = "Ignorar";
        public string? ProfileName { get; set; }
        public IReadOnlyList<ExternalScoringRange>? WeightedRanges { get; set; }
    }

    public class ExternalScoringRange
    {
        public int FromQuestion { get; set; }
        public int ToQuestion { get; set; }
        public decimal PuntosCorrecta { get; set; }
    }

    public class ExternalScoreRow
    {
        public int Ranking { get; set; }
        public string Litho { get; set; } = "";
        public string Codigo { get; set; } = "";
        public string Tema { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Carrera { get; set; } = "";
        public string Modalidad { get; set; } = "";
        public string TipoModalidad { get; set; } = "";
        public int Correctas { get; set; }
        public int Incorrectas { get; set; }
        public int Blancas { get; set; }
        public int Anuladas { get; set; }
        public int Multiples { get; set; }
        public decimal Puntaje { get; set; }
        public decimal? Vigesimal { get; set; }
        public bool EncontradoEnBD { get; set; }
        public bool EsIngresante { get; set; }
        public bool NoSePresento { get; set; }
        public string? Observacion { get; set; }
    }

    public class ExternalScoringResult
    {
        public int TotalPreguntas { get; set; }
        public int TotalTemas { get; set; }
        public int TotalPostulantes { get; set; }
        public int TotalIngresantes { get; set; }
        public int TotalNoPresentados { get; set; }
        public int TotalConErrores { get; set; }
        public int TotalSinCoincidencia { get; set; }
        public List<ExternalScoreRow> Rows { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public interface IExamProcessingService
    {
        ExternalScoringResult ProcessExternal(
            Stream keyStream,
            Stream answersStream,
            Stream? identificationStream,
            Stream? bdStream,
            string? bdFileName,
            ExternalScoringParameters parameters);

        byte[] BuildExternalExcel(ExternalScoringResult data, string profileName, string titulo);

        byte[] BuildPostulantsTemplate();
    }
}
