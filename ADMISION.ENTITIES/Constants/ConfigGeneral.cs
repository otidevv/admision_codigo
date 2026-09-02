using System;
using System.Collections.Generic;
using System.Text;

namespace ADMISION.ENTITIES.Constants
{
    public static class ConfigGeneral
    {
        public const string NombreInstitucion = "NombreInstitucion";
        public const string Dependencia = "Dependencia";
        public const string Direccion = "Direccion";
        public const string Telefono = "Telefono";
        public const string Celular = "Celular";
        public const string CorreoInstitucional = "CorreoInstitucional";
        public const string Ruc = "Ruc";

        public const string Facebook = "Facebook";
        public const string Instagram = "Instagram";
        public const string Youtube = "Youtube";
        public const string Tiktok = "Tiktok";
        public const string Twitter = "Twitter";

        public const string HorarioAtencion = "HorarioAtencion";

        public const string Director = "Director";
        public const string DirecctorComision = "DirecctorComision";

        public const string LogoUrl = "LogoUrl";
        public const string IconUrl = "IconUrl";
        public const string ColorPrimario = "ColorPrimario";
        public const string ColorSecundario = "ColorSecundario";
        public const string MapUrl = "MapUrl";
        public const string ResolVacancies = "ResolVacancies";

        public const string SmtpHost = "SmtpHost";
        public const string SmtpPort = "SmtpPort";
        public const string SmtpEnableSsl = "SmtpEnableSsl";
        public const string SmtpSenderName = "SmtpSenderName";
        public const string SmtpEmail = "SmtpEmail";
        public const string SmtpPassword = "SmtpPassword";

        // Método para obtener valores por defecto (Seed inicial)
        public static Dictionary<string, string> DefaultValues => new()
        {
            { NombreInstitucion, "UNIVERSIDAD NACIONAL AMAZÓNICA DE MADRE DE DIOS" },
            { Dependencia, "DIRECCIÓN DE ADMISIÓN" },
            { Direccion, "Av. Jorge Chávez N° 1160" },
            { Telefono, "982 845 769" },
            { Celular, "993 170 418" },
            { CorreoInstitucional, "admision@unamad.edu.pe" },
            { Ruc, "20526917295" },
            { ResolVacancies, "" },

            { Facebook, "https://facebook.com/admision" },
            { Instagram, "https://instagram.com/admision" },
            { Youtube, "" },
            { Tiktok, "" },
            { Twitter, "" },

            { HorarioAtencion, "Lunes a Viernes 8:00 AM - 5:00 PM" },

            { Director, "Nombre del Director" },
            { DirecctorComision, "Nombre del Director de la Comision" },

            { LogoUrl, "/img/logo.png" },
            { IconUrl, "/img/unamad.png" },
            { ColorPrimario, "#1d4ed8" },
            { ColorSecundario, "#9333ea" },
            { MapUrl, "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d1946.9537310505057!2d-69.21030156117385!3d-12.588350179189826!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x917b49441850fe49%3A0x2881b0658744e313!2sUniversidad%20Nacional%20Amaz%C3%B3nica%20de%20Madre%20de%20Dios%2C%20Puerto%20Maldonado%2017001!5e0!3m2!1ses-419!2spe!4v1782517052002!5m2!1ses-419!2spe" },

            { SmtpHost, "smtp.gmail.com" },
            { SmtpPort, "587" },
            { SmtpEnableSsl, "true" },
            { SmtpSenderName, "Dirección de Admisión" },
            { SmtpEmail, "admision@unamad.edu.pe" },
            { SmtpPassword, "" }
        };
    }
}