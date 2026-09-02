namespace ADMISION.Models.ViewModels.Reports
{
    public class GeneralReportFilter
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? TypePostulantId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GeneralReportViewModel
    {
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public Guid? ModalityId { get; set; }
        public string? ModalityName { get; set; }
        public Guid? TypeModalityId { get; set; }
        public string? TypeModalityName { get; set; }
        public Guid? TypePostulantId { get; set; }
        public string? TypePostulantName { get; set; }

        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;

        public List<GeneralReportItem> Items { get; set; } = new();
    }

    public class GeneralReportItem
    {
        public string TipoExamen { get; set; } = string.Empty;
        public string Modalidad { get; set; } = string.Empty;
        public string FechaInscripcion { get; set; } = string.Empty;
        public string CodigoPostulante { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public string FechaNacimiento { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string EstadoCivil { get; set; } = string.Empty;
        public string TieneDiscapacidad { get; set; } = string.Empty;
        public string Discapacidad { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string CodigoCarrera { get; set; } = string.Empty;
        public string CarreraProfesional { get; set; } = string.Empty;
        public string Tema { get; set; } = string.Empty;
        public string Ciclo { get; set; } = string.Empty;
        public string UbigeoColegio { get; set; } = string.Empty;
        public string NombreColegio { get; set; } = string.Empty;
        public string DistritoColegio { get; set; } = string.Empty;
        public string ProvinciaColegio { get; set; } = string.Empty;
        public string DepartamentoColegio { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string Ubigeo { get; set; } = string.Empty;
        public string DistritoProcedencia { get; set; } = string.Empty;
        public string ProvinciaProcedencia { get; set; } = string.Empty;
        public string DepartamentoProcedencia { get; set; } = string.Empty;
    }
}
