using System.Collections.Generic;

namespace ADMISION.ENTITIES.Constants
{
    /// <summary>
    /// Opción para selects/dropdowns. Tipo público (no anonymous) para que
    /// Razor views lo acceda via dynamic sin problemas de visibilidad.
    /// </summary>
    public sealed class SelectOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        public SelectOption() { }
        public SelectOption(string value, string label)
        {
            Value = value;
            Label = label;
        }
    }

    public static class AppConstants
    {
        public static class Roles
        {
            public const string Admin = "Administrador";
            public const string Soporte = "Soporte";
            public const string SuperAdmin = "SuperAdmin";
            public const string Consultor = "Consultor";
            public const string ApiConsumer = "ApiConsumer";
        }
        public static class Usuarios
        {
            public const string Activo = "Activo";
            public const string Bloqueado = "Bloqueado";
            public const string Inactivo = "Inactivo";
        }
        public static class InscripcionState
        {
            // Estados del expediente de inscripción (no confundir con el resultado del examen).
            //   Pendiente: recién inscrito, documentos en revisión.
            //   Aprobado : documentos verificados correctamente.
            //   Observado: hay observaciones en los documentos.
            //   Rechazado: documentos rechazados.
            //   Retirado : el postulante se retiró del proceso.
            public const string Pendiente = "Pendiente";
            public const string Observado = "Observado";
            public const string Aprobado = "Aprobado";
            public const string Rechazado = "Rechazado";
            public const string Retirado = "Retirado";
        }

        public static class FileExtensions
        {
            public static readonly List<string> Allowed = new List<string> { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".doc", ".docx", ".xls", ".xlsx" };
        }

        public static class OtherFileCategory
        {
            public const string Reglamento = "Reglamento";
            public const string Temario = "Temario";
            public const string Otros = "Otros";
        }

        public static class SchedulePhase
        {
            public const string Inscripcion = "Inscripcion";
            public const string Examen = "Examen";
            public const string Resultados = "Resultados";
            public const string EntregaRequisitos = "EntregaRequisitos";
            public const string EntregaConstancia = "EntregaConstancia";

            public static readonly Dictionary<string, string> Labels = new()
            {
                [Inscripcion] = "Inscripciones",
                [Examen] = "Fecha de Examen",
                [Resultados] = "Publicación de Resultados",
                [EntregaRequisitos] = "Entrega de Requisitos de Ingresantes",
                [EntregaConstancia] = "Entrega de Constancia de Ingreso"
            };

            public static readonly Dictionary<string, string> Icons = new()
            {
                [Inscripcion] = "ti-pencil",
                [Examen] = "ti-file-pencil",
                [Resultados] = "ti-speakerphone",
                [EntregaRequisitos] = "ti-folder-open",
                [EntregaConstancia] = "ti-certificate"
            };

            public static readonly string[] Order = new[]
            {
                Inscripcion, Examen, Resultados, EntregaRequisitos, EntregaConstancia
            };

            public static List<SelectOption> GetOptions() => new()
            {
                new(Inscripcion, Labels[Inscripcion]),
                new(Examen, Labels[Examen]),
                new(Resultados, Labels[Resultados]),
                new(EntregaRequisitos, Labels[EntregaRequisitos]),
                new(EntregaConstancia, Labels[EntregaConstancia])
            };
        }

        public static class RequirementStage
        {
            public const string Postulation = "Postulation";
            public const string Entry = "Entry";
            public const string Both = "Both";

            public static readonly Dictionary<string, string> Labels = new()
            {
                [Postulation] = "Para Postular",
                [Entry] = "Para Ingreso",
                [Both] = "Postulación e Ingreso"
            };

            public static List<SelectOption> GetOptions() => new()
            {
                new(Postulation, Labels[Postulation]),
                new(Entry, Labels[Entry]),
                new(Both, Labels[Both])
            };
        }

        public static class ModalityBadge
        {
            public const string Principal = "Principal";
            public const string Dirimencia = "Dirimencia";
            public const string Especial = "Especial";

            public static List<SelectOption> GetOptions() => new()
            {
                new(Principal, "Principal"),
                new(Dirimencia, "Dirimencia"),
                new(Especial, "Especial")
            };
        }

        public static class ModalityIcon
        {
            public static readonly Dictionary<string, string> Catalog = new()
            {
                ["fa-solid fa-file-lines"] = "Documento",
                ["fa-solid fa-right-left"] = "Traslado Externo",
                ["fa-solid fa-arrows-up-down"] = "Traslado Interno",
                ["fa-solid fa-trophy"] = "Deportistas",
                ["fa-solid fa-wheelchair"] = "Discapacidad",
                ["fa-solid fa-star"] = "Primeros Puestos",
                ["fa-solid fa-graduation-cap"] = "Graduados/Titulados"
            };

            public static List<SelectOption> GetOptions() =>
                Catalog.Select(kvp => new SelectOption(kvp.Key, kvp.Value)).ToList();
        }

        /// <summary>
        /// Orden jerárquico de las modalidades usado para generar el número de
        /// ingreso (últimos 3 dígitos del código de estudiante) en el consolidado.
        /// Coincide con la columna Orden de la tabla Modality.
        /// Cualquier otro valor distinto a estos 5 se procesa al final.
        /// </summary>
        public static class ConsolidadoModalityOrden
        {
            public const int Ordinario = 1;
            public const int Secundaria = 2;
            public const int Dirimencia = 3;
            public const int Cepre = 4;
            public const int MedicinaHumana = 5;
        }

        public static class ConsolidadoMapping
        {
            public const string UbigeoDefault = "-";

            public static string MapGenero(string? genero)
            {
                if (string.IsNullOrWhiteSpace(genero)) return "";
                var g = genero.Trim().ToUpperInvariant();
                return g is "M" or "MASCULINO" ? "1" : g is "F" or "FEMENINO" ? "2" : "";
            }

            public static string MapEstadoCivil(string? estadoCivil)
            {
                if (string.IsNullOrWhiteSpace(estadoCivil)) return "";
                return estadoCivil.Trim().ToUpperInvariant() == "SOLTERO" ? "0" : "1";
            }

            public static string MapTipoDocumento(string? tipoDocumento)
            {
                if (string.IsNullOrWhiteSpace(tipoDocumento)) return "";
                var td = tipoDocumento.Trim().ToUpperInvariant();
                return td is "DNI" ? "2" : td is "CE" or "CARNET DE EXTRANJERIA" ? "3" : "";
            }

            public static class TipoObservacion
            {
public const string NoPresentoRequisitosCompletos = "1";
                public const string NoPresentoRequisitos = "2";
                public const string Ninguna = "3";
                public const string Renuncia = "4";

                public const string LabelNoPresentoRequisitosCompletos = "NO PRESENTÓ REQUISITOS COMPLETOS";
                public const string LabelNoPresentoRequisitos = "NO PRESENTÓ REQUISITOS";
                public const string LabelNinguna = "NINGUNA";
                public const string LabelRenuncia = "RENUNCIA";

                public static readonly Dictionary<string, string> Labels = new()
                {
                    [NoPresentoRequisitosCompletos] = LabelNoPresentoRequisitosCompletos,
                    [NoPresentoRequisitos] = LabelNoPresentoRequisitos,
                    [Ninguna] = LabelNinguna,
                    [Renuncia] = LabelRenuncia
                };

                public static List<SelectOption> GetOptions() => new()
                {
                    new(Ninguna, LabelNinguna),
                    new(NoPresentoRequisitosCompletos, LabelNoPresentoRequisitosCompletos),
                    new(NoPresentoRequisitos, LabelNoPresentoRequisitos),
                    new(Renuncia, LabelRenuncia)
                };
                }
        }

    }
}
