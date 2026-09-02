using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.System;
using ADMISION.ENTITIES.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ADMISION.Data
{
    public static class DbInitializer
    {
        public static void Initialize(
            AppDbContext context,
            ADMISION.Services.Interfaces.IPasswordHasher hasher,
            IConfiguration configuration,
            ILogger logger)
        {
            logger.LogInformation("--- Starting DB Initialization ---");
            context.Database.Migrate();
            logger.LogInformation("--- Database Migration Completed ---");

            SeedRoles(context);
            SeedAdmin(context, hasher, configuration, logger);
            SeedConfig(context);
            SeedDisabilityTypes(context);
            UbigeoInitializer.Initialize(context);
            UniversityInitializer.Initialize(context);
        }

        private static void SeedRoles(AppDbContext context)
        {
            if (context.Rols.Any()) return;

            var roles = new Rols[]
            {
                new Rols { Id = Guid.NewGuid(), Name = AppConstants.Roles.Admin, State = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new Rols { Id = Guid.NewGuid(), Name = AppConstants.Roles.Soporte, State = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new Rols { Id = Guid.NewGuid(), Name = AppConstants.Roles.SuperAdmin, State = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new Rols { Id = Guid.NewGuid(), Name = AppConstants.Roles.Consultor, State = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new Rols { Id = Guid.NewGuid(), Name = AppConstants.Roles.ApiConsumer, State = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" }
            };

            context.Rols.AddRange(roles);
            context.SaveChanges();
        }

        private static void SeedAdmin(
            AppDbContext context,
            ADMISION.Services.Interfaces.IPasswordHasher hasher,
            IConfiguration configuration,
            ILogger logger)
        {
            // Si ya existe al menos un usuario, no crear el administrador inicial.
            if (context.Users.Any())
                return;

            var adminPassword = configuration["Admin:InitialPassword"];
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                adminPassword = GenerateRandomPassword(20);
                logger.LogWarning(
                    "No se definió 'Admin:InitialPassword'. Contraseña temporal generada para 'admin': {Password} — cámbiela inmediatamente.",
                    adminPassword);
            }

            var adminUser = new Users
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                Password = hasher.HashPassword(adminPassword),
                Name = "Administrador",
                FirstNameFather = "Sistema",
                FirstNameMother = "Admision",
                FullName = "Administrador Sistema",
                Document = "00000000",
                DocumentType = "DNI",
                PhoneNumber = "000000000",
                Email = "admin@admision.com",
                Genero = "M",
                IsDisabled = AppConstants.Usuarios.Activo,
                Birthdate = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            };

            context.Users.Add(adminUser);
            context.SaveChanges();

            var adminRole = context.Rols.FirstOrDefault(r => r.Name == AppConstants.Roles.SuperAdmin);

            if (adminRole != null)
            {
                context.UserRols.Add(new UserRol
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUser.Id,
                    RolsId = adminRole.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "System"
                });

                context.SaveChanges();
            }
        }

        private static void SeedConfig(AppDbContext context)
        {
            foreach (var item in ConfigGeneral.DefaultValues)
            {
                if (!context.Configs.Any(c => c.Key == item.Key))
                {
                    context.Configs.Add(new Config
                    {
                        Id = Guid.NewGuid(),
                        Key = item.Key,
                        Value = item.Value,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = "System"
                    });
                }
            }

            context.SaveChanges();
        }

        private static string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*";
            var bytes = RandomNumberGenerator.GetBytes(length);
            var result = new char[length];
            for (int i = 0; i < length; i++) result[i] = chars[bytes[i] % chars.Length];
            return new string(result);
        }

        private static void SeedDisabilityTypes(AppDbContext context)
        {
            if (context.DisabilityTypes.Any()) return;

            var types = new ADMISION.ENTITIES.Models.Postulant.DisabilityType[]
            {
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Discapacidad Motriz", Description = "Condición que afecta el movimiento o la movilidad del cuerpo.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Discapacidad Visual", Description = "Limitación total o parcial de la visión.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Visual y Esquema Corporal", Description = "Dificultades relacionadas con la percepción visual y la orientación corporal.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Disminuidos Visuales", Description = "Personas con baja visión que presentan limitaciones visuales significativas.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Discapacidad Auditiva", Description = "Limitación total o parcial de la audición.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Autismo", Description = "Trastorno del neurodesarrollo caracterizado por dificultades en la comunicación e interacción social.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Discapacidad Mental", Description = "Condiciones relacionadas con alteraciones en la salud mental.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Parálisis Cerebral", Description = "Trastorno del movimiento y la postura causado por daño en el cerebro en desarrollo.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Discapacidad Intelectual", Description = "Limitaciones significativas en el funcionamiento intelectual y en habilidades adaptativas.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Sordoceguera", Description = "Condición que combina discapacidad auditiva y visual.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "No Cuenta con Información", Description = "No se dispone de información sobre el tipo de discapacidad.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Otros", Description = "Otros tipos de discapacidad no especificados.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Sindrome de Asperger", Description = "Trastorno del espectro autista caracterizado por dificultades sociales y patrones de comportamiento repetitivos.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Hemiplejia no Identificada", Description = "Parálisis de un lado del cuerpo de origen no especificado.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Estenosis Congénita de la Válvula Aortica", Description = "Condición congénita que provoca estrechamiento de la válvula aórtica del corazón.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Multidiscapacidad", Description = "Presencia de dos o más discapacidades en una misma persona.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Discapacidad Fisica", Description = "Condición que afecta la movilidad o funciones físicas del cuerpo.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Transtorno del Espectro Autista", Description = "Trastorno del neurodesarrollo que afecta la comunicación y el comportamiento.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "T. por Déficit de Atención con Hiperactividad", Description = "Trastorno caracterizado por inatención, hiperactividad e impulsividad.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "T. Especifico del Aprendizaje", Description = "Dificultades persistentes en habilidades académicas como lectura, escritura o matemáticas.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "T. Mentales y del Comportamiento", Description = "Trastornos relacionados con la salud mental y conductual.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Enfermedades Raras", Description = "Enfermedades poco frecuentes que pueden generar discapacidad.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Talla Baja", Description = "Condición caracterizada por estatura significativamente inferior al promedio.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Talento", Description = "Capacidad sobresaliente en una o más áreas específicas.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" },
                new ADMISION.ENTITIES.Models.Postulant.DisabilityType { Id = Guid.NewGuid(), Name = "Superdotación", Description = "Altas capacidades intelectuales o cognitivas significativamente superiores al promedio.", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "System" }
            };

            context.DisabilityTypes.AddRange(types);
            context.SaveChanges();
        }
    }
}