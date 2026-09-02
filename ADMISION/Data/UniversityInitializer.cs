using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADMISION.Data
{
    /// <summary>
    /// Siembra el catálogo de universidades del Perú licenciadas por SUNEDU.
    /// Es aditivo: inserta sólo los códigos (Acronym) faltantes, por lo que puede
    /// re-ejecutarse sin romper FKs existentes.
    /// </summary>
    public static class UniversityInitializer
    {
        private const string Publica = "Pública";
        private const string Privada = "Privada";

        public static void Initialize(AppDbContext context)
        {
            var catalog = Catalog();
            var existing = context.Universities
                .Select(u => u.Acronym)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missing = catalog
                .Where(u => !existing.Contains(u.Acronym))
                .Select(u => new University
                {
                    Id = Guid.NewGuid(),
                    Name = u.Name,
                    Acronym = u.Acronym,
                    Kind = u.Kind,
                    Region = u.Region,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "System"
                })
                .ToList();

            if (missing.Count == 0)
            {
                Console.WriteLine("Universities catalog is complete. Nothing to seed.");
                return;
            }

            context.Universities.AddRange(missing);
            context.SaveChanges();
            Console.WriteLine($"Seeded {missing.Count} universities.");
        }

        private record Seed(string Name, string Acronym, string Kind, string? Region);

        private static IReadOnlyList<Seed> Catalog() => new List<Seed>
        {
            // ── Universidades Nacionales (Públicas) ─────────────────────────────
            new("Universidad Nacional Amazónica de Madre de Dios", "UNAMAD", Publica, "Madre de Dios"),
            new("Universidad Nacional Mayor de San Marcos", "UNMSM", Publica, "Lima"),
            new("Universidad Nacional de Ingeniería", "UNI", Publica, "Lima"),
            new("Universidad Nacional Agraria La Molina", "UNALM", Publica, "Lima"),
            new("Universidad Nacional Federico Villarreal", "UNFV", Publica, "Lima"),
            new("Universidad Nacional de Educación Enrique Guzmán y Valle", "UNE", Publica, "Lima"),
            new("Universidad Nacional del Callao", "UNAC", Publica, "Callao"),
            new("Universidad Nacional Tecnológica de Lima Sur", "UNTELS", Publica, "Lima"),
            new("Universidad Nacional de Cañete", "UNDC", Publica, "Lima"),
            new("Universidad Nacional de Barranca", "UNAB", Publica, "Lima"),
            new("Universidad Nacional José Faustino Sánchez Carrión", "UNJFSC", Publica, "Lima"),
            new("Universidad Nacional Autónoma de Chota", "UNACH", Publica, "Cajamarca"),
            new("Universidad Nacional Autónoma de Huanta", "UNAH", Publica, "Ayacucho"),
            new("Universidad Nacional Autónoma Altoandina de Tarma", "UNAAT", Publica, "Junín"),
            new("Universidad Nacional de Trujillo", "UNT", Publica, "La Libertad"),
            new("Universidad Nacional de Cajamarca", "UNC", Publica, "Cajamarca"),
            new("Universidad Nacional de Piura", "UNP", Publica, "Piura"),
            new("Universidad Nacional de Tumbes", "UNT-TUMBES", Publica, "Tumbes"),
            new("Universidad Nacional de Frontera", "UNF", Publica, "Piura"),
            new("Universidad Nacional de Jaén", "UNJ", Publica, "Cajamarca"),
            new("Universidad Nacional Pedro Ruiz Gallo", "UNPRG", Publica, "Lambayeque"),
            new("Universidad Nacional Toribio Rodríguez de Mendoza", "UNTRM", Publica, "Amazonas"),
            new("Universidad Nacional Santiago Antúnez de Mayolo", "UNASAM", Publica, "Áncash"),
            new("Universidad Nacional del Santa", "UNS", Publica, "Áncash"),
            new("Universidad Nacional del Centro del Perú", "UNCP", Publica, "Junín"),
            new("Universidad Nacional de Huancavelica", "UNH", Publica, "Huancavelica"),
            new("Universidad Nacional Daniel Alcides Carrión", "UNDAC", Publica, "Pasco"),
            new("Universidad Nacional Hermilio Valdizán", "UNHEVAL", Publica, "Huánuco"),
            new("Universidad Nacional Agraria de la Selva", "UNAS", Publica, "Huánuco"),
            new("Universidad Nacional Intercultural de la Selva Central Juan Santos Atahualpa", "UNISCJSA", Publica, "Junín"),
            new("Universidad Nacional San Luis Gonzaga", "UNICA", Publica, "Ica"),
            new("Universidad Nacional Autónoma de Huanta", "UNAH-AYA", Publica, "Ayacucho"),
            new("Universidad Nacional de San Cristóbal de Huamanga", "UNSCH", Publica, "Ayacucho"),
            new("Universidad Nacional de San Antonio Abad del Cusco", "UNSAAC", Publica, "Cusco"),
            new("Universidad Nacional Intercultural de Quillabamba", "UNIQ", Publica, "Cusco"),
            new("Universidad Nacional de San Agustín de Arequipa", "UNSA", Publica, "Arequipa"),
            new("Universidad Nacional de Moquegua", "UNAM", Publica, "Moquegua"),
            new("Universidad Nacional Jorge Basadre Grohmann", "UNJBG", Publica, "Tacna"),
            new("Universidad Nacional de San Martín", "UNSM", Publica, "San Martín"),
            new("Universidad Nacional de la Amazonía Peruana", "UNAP", Publica, "Loreto"),
            new("Universidad Nacional de Ucayali", "UNU", Publica, "Ucayali"),
            new("Universidad Nacional Intercultural de la Amazonía", "UNIA", Publica, "Ucayali"),
            new("Universidad Nacional de Juliaca", "UNAJ", Publica, "Puno"),
            new("Universidad Nacional del Altiplano", "UNAP-PUNO", Publica, "Puno"),
            new("Universidad Nacional Micaela Bastidas de Apurímac", "UNAMBA", Publica, "Apurímac"),
            new("Universidad Nacional José María Arguedas", "UNAJMA", Publica, "Apurímac"),
            new("Universidad Nacional de Música", "UNM", Publica, "Lima"),
            new("Universidad Nacional Diego Quispe Tito", "UNDQT", Publica, "Cusco"),
            new("Universidad Nacional Autónoma de Alto Amazonas", "UNAAA", Publica, "Loreto"),
            new("Universidad Nacional Ciro Alegría", "UNCA", Publica, "La Libertad"),
            new("Universidad Nacional Intercultural de la Selva Central", "UNISC", Publica, "Junín"),
            new("Universidad Nacional Intercultural Fabiola Salazar Leguía de Bagua", "UNIFSLB", Publica, "Amazonas"),
            new("Universidad Nacional Tecnológica de San Juan de Lurigancho", "UNTLL", Publica, "Lima"),

            // ── Universidades Privadas ───────────────────────────────────────────
            new("Pontificia Universidad Católica del Perú", "PUCP", Privada, "Lima"),
            new("Universidad Peruana Cayetano Heredia", "UPCH", Privada, "Lima"),
            new("Universidad de Lima", "ULIMA", Privada, "Lima"),
            new("Universidad del Pacífico", "UP", Privada, "Lima"),
            new("Universidad de Piura", "UDEP", Privada, "Piura"),
            new("Universidad ESAN", "ESAN", Privada, "Lima"),
            new("Universidad Peruana de Ciencias Aplicadas", "UPC", Privada, "Lima"),
            new("Universidad San Ignacio de Loyola", "USIL", Privada, "Lima"),
            new("Universidad de San Martín de Porres", "USMP", Privada, "Lima"),
            new("Universidad Ricardo Palma", "URP", Privada, "Lima"),
            new("Universidad Femenina del Sagrado Corazón", "UNIFÉ", Privada, "Lima"),
            new("Universidad Antonio Ruiz de Montoya", "UARM", Privada, "Lima"),
            new("Universidad Científica del Sur", "UCSUR", Privada, "Lima"),
            new("Universidad Privada Norbert Wiener", "UPNW", Privada, "Lima"),
            new("Universidad Privada del Norte", "UPN", Privada, "Lima"),
            new("Universidad Tecnológica del Perú", "UTP", Privada, "Lima"),
            new("Universidad César Vallejo", "UCV", Privada, "La Libertad"),
            new("Universidad Continental", "UC", Privada, "Junín"),
            new("Universidad Privada de Tacna", "UPT", Privada, "Tacna"),
            new("Universidad Privada Antenor Orrego", "UPAO", Privada, "La Libertad"),
            new("Universidad Católica Santo Toribio de Mogrovejo", "USAT", Privada, "Lambayeque"),
            new("Universidad Católica de Santa María", "UCSM", Privada, "Arequipa"),
            new("Universidad Católica San Pablo", "UCSP", Privada, "Arequipa"),
            new("Universidad Católica de Trujillo Benedicto XVI", "UCT", Privada, "La Libertad"),
            new("Universidad Católica Sedes Sapientiae", "UCSS", Privada, "Lima"),
            new("Universidad Católica Los Ángeles de Chimbote", "ULADECH", Privada, "Áncash"),
            new("Universidad Andina del Cusco", "UAC", Privada, "Cusco"),
            new("Universidad Andina Néstor Cáceres Velásquez", "UANCV", Privada, "Puno"),
            new("Universidad Alas Peruanas", "UAP", Privada, "Lima"),
            new("Universidad San Pedro", "USP", Privada, "Áncash"),
            new("Universidad Peruana Unión", "UPeU", Privada, "Lima"),
            new("Universidad Marcelino Champagnat", "UMCH", Privada, "Lima"),
            new("Universidad Peruana de Las Américas", "UPA", Privada, "Lima"),
            new("Universidad Le Cordon Bleu", "ULCB", Privada, "Lima"),
            new("Universidad Autónoma del Perú", "UA", Privada, "Lima"),
            new("Universidad Autónoma de Ica", "UAI", Privada, "Ica"),
            new("Universidad Jaime Bausate y Meza", "UJBM", Privada, "Lima"),
            new("Universidad ESAN", "ESAN2", Privada, "Lima"),
            new("Universidad Peruana de Ingeniería", "UPI", Privada, "Lima"),
            new("Universidad Peruana Los Andes", "UPLA", Privada, "Junín"),
            new("Universidad Señor de Sipán", "USS", Privada, "Lambayeque"),
            new("Universidad Privada Juan Pablo II", "UPJP2", Privada, "Lima"),
            new("Universidad Privada del Valle Grande", "UPVG", Privada, "Ica"),
            new("Universidad de Ciencias y Humanidades", "UCH", Privada, "Lima"),
            new("Universidad de Ingeniería y Tecnología", "UTEC", Privada, "Lima"),
            new("Universidad para el Desarrollo Andino", "UDEA", Privada, "Huancavelica"),
            new("Universidad Tecnológica de los Andes", "UTEA", Privada, "Apurímac"),
            new("Universidad Privada San Juan Bautista", "UPSJB", Privada, "Lima"),
            new("Universidad Privada SISE", "SISE", Privada, "Lima"),
            new("Universidad ISIL", "ISIL", Privada, "Lima"),

            // Catch-all
            new("Otra universidad / No listada", "OTRA", Privada, null)
        };
    }
}
