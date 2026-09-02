namespace ADMISION.Models.ViewModels.Reports
{
    public class CepreReportViewModel
    {
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public Guid? VersionId { get; set; }
        public string? VersionLabel { get; set; }
        public int TotalRecords { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
        public List<CepreReportItem> Items { get; set; } = new();
    }

    public class CepreReportItem
    {
        public int Nro { get; set; }
        public string Ciclo { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string TDocumento { get; set; } = string.Empty;
        public string Apaterno { get; set; } = string.Empty;
        public string Amaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string ApellidosNombres { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public string FechaNacimiento { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string EstadoCivil { get; set; } = string.Empty;
        public string AnioEgreso { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string Colegio { get; set; } = string.Empty;
        public string NombreColegio { get; set; } = string.Empty;
        public string UbigeoColegio { get; set; } = string.Empty;
        public string DireccionColegio { get; set; } = string.Empty;
        public string Ubigeo { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
        public string LugarNacimiento { get; set; } = string.Empty;
        public string Modalidad { get; set; } = string.Empty;
        public string CodigoCarrera { get; set; } = string.Empty;
        public string CarreraProfesional { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
        public string ModalidadPago { get; set; } = string.Empty;
        public decimal? Monto { get; set; }
        public decimal? Puntaje01 { get; set; }
        public decimal? Nota01 { get; set; }
        public decimal? Puntaje02 { get; set; }
        public decimal? Nota02 { get; set; }
        public decimal? Puntaje03 { get; set; }
        public decimal? Nota03 { get; set; }
        public decimal? NotaFinal { get; set; }
        public decimal? Puntaje { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
