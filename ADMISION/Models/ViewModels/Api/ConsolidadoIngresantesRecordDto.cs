namespace admision.Models.ViewModels.Api
{
    public class ConsolidadoIngresantesRecordDto
    {
        public string CodigoEstudiante { get; set; } = default!;
        public string CodigoCarrera { get; set; } = default!;
        public string SegundaCarrera { get; set; } = default!;
        public string Semestre { get; set; } = default!;
        public string Nombres { get; set; } = default!;
        public string Paterno { get; set; } = default!;
        public string Materno { get; set; } = default!;
        public string DType { get; set; } = default!;
        public string Dni { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Celular { get; set; } = default!;
        public string Direccion { get; set; } = default!;
        public string FechaNacimiento { get; set; }
        public string Sexo { get; set; } = default!;
        public string EstadoCivil { get; set; } = default!;
        public string Ubigeo { get; set; } = default!;
        public string TipoPostulante { get; set; } = default!;
        public string TipoObs { get; set; } = default!;
        public string? Observaciones { get; set; }
        public int Nro { get; set; }
    }
}
