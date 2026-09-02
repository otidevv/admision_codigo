using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Biometrics;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.ENTITIES.Models.Users;
using ADMISION.Models.ViewModels.Public;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

namespace ADMISION.Services.Implementations
{
    public class InscriptionService : IInscriptionService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;
        private readonly IPostulantCodeService _codeService;
        private readonly INotificationService _notifications;
        private readonly IExternalApiService _externalApi;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<InscriptionService> _logger;

        public InscriptionService(
            AppDbContext context,
            IFileService files,
            IPostulantCodeService codeService,
            INotificationService notifications,
            IExternalApiService externalApi,
            IWebHostEnvironment env,
            ILogger<InscriptionService> logger)
        {
            _context = context;
            _files = files;
            _codeService = codeService;
            _notifications = notifications;
            _externalApi = externalApi;
            _env = env;
            _logger = logger;
        }

        public async Task<InscriptionRegisterResult> RegisterAsync(InscriptionRegisterInput input, CancellationToken ct = default)
        {
            var model = input.Model;
            var createdBy = input.CreatedBy;
            string? currentFileContext = null; // se actualiza antes de cada SaveFile para que un InvalidFileException sepa de qué archivo habla.

            // El `using` se asegura del rollback si no llegamos a CommitAsync.
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var now = DateTimeOffset.UtcNow;

                // 1. User & Postulant (get-or-create)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Document == model.DocumentNumber, ct);
                Guid postulantId;

                if (user == null)
                {
                    user = new Users
                    {
                        Id = Guid.NewGuid(),
                        Document = model.DocumentNumber,
                        DocumentType = model.DocumentType,
                        Name = model.Name.ToUpper().Trim(),
                        FirstNameFather = model.FatherSurname.ToUpper().Trim(),
                        FirstNameMother = model.MotherSurname.ToUpper().Trim(),
                        FullName = $"{model.Name} {model.FatherSurname} {model.MotherSurname}".ToUpper().Trim(),
                        PhoneNumber = model.PhoneNumber,
                        Email = model.Email ?? $"{model.DocumentNumber}@unamad.edu.pe",
                        Genero = model.Genero ?? "M",
                        CivilStatus = model.CivilStatus,
                        Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.ToUpper().Trim(),
                        Birthdate = new DateTimeOffset(model.BirthDate, TimeSpan.Zero),
                        CreatedAt = now,
                        CreatedBy = createdBy
                    };
                    _context.Users.Add(user);

                    var postulant = new Postulant
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CreatedAt = now,
                        CreatedBy = createdBy
                    };
                    _context.Postulants.Add(postulant);
                    postulantId = postulant.Id;
                }
                else
                {
                    var postulant = await _context.Postulants.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
                    if (postulant == null)
                    {
                        postulant = new Postulant
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            CreatedAt = now,
                            CreatedBy = createdBy
                        };
                        _context.Postulants.Add(postulant);
                        postulantId = postulant.Id;
                    }
                    else
                    {
                        postulantId = postulant.Id;
                    }

                    if (!string.IsNullOrEmpty(model.ConadisNumber))
                    {
                        postulant.ConadisNumber = model.ConadisNumber;
                        _context.Postulants.Update(postulant);
                    }

                    if (!string.IsNullOrWhiteSpace(model.Address))
                    {
                        user.Address = model.Address.ToUpper().Trim();
                        _context.Users.Update(user);
                    }
                }

                // 1b. Anulación vigente: el postulante tiene restringida la inscripción.
                var hasActiveAnnulment = await _context.Annulments
                    .AnyAsync(a => a.PostulantId == postulantId && a.StartDate <= now && a.EndDate >= now, ct);
                if (hasActiveAnnulment)
                {
                    return new InscriptionRegisterResult
                    {
                        Outcome = InscriptionOutcome.Blocked,
                        Message = "Tu inscripción se encuentra restringida por una anulación vigente. Debes consultar con la administración."
                    };
                }

                // 1c. Consulta de penalizados (API "CONSULTA_PENALIZADOS" por DNI):
                //     bloquea si el postulante figura Sancionado/Expulsado o ya está en la misma carrera.
                if (string.Equals(model.DocumentType, "DNI", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(model.DocumentNumber))
                {
                    var careerName = await _context.Careers.AsNoTracking()
                        .Where(c => c.Id == model.CareerId)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync(ct);

                    var sanctions = await _externalApi.CheckSanctionsAsync(
                        model.DocumentNumber, careerName ?? string.Empty, input.RemoteIp, ct);

                    if (sanctions.Blocked)
                    {
                        _logger.LogInformation(
                            "Inscripción bloqueada por penalizados: DNI {Dni}, estado {Status}, carrera {Career}, postula a {CareerApplied}",
                            model.DocumentNumber,
                            sanctions.StudentStatus ?? "-",
                            sanctions.CareerName ?? sanctions.StudentCareer ?? "-",
                            careerName ?? "-");

                        return new InscriptionRegisterResult
                        {
                            Outcome = InscriptionOutcome.Blocked,
                            Message = sanctions.Message
                                ?? "No es posible registrar tu inscripción. Comunícate con la Dirección de Admisión para más información."
                        };
                    }

                    if (!string.IsNullOrEmpty(sanctions.Error))
                    {
                        _logger.LogWarning(
                            "Consulta de penalizados no concluyente para DNI {Dni}: {Error}",
                            model.DocumentNumber, sanctions.Error);
                    }
                }

                // 2. Disabilities
                if (model.DisabilityTypeIds != null && model.DisabilityTypeIds.Any())
                {
                    var existing = _context.PostulantDisabilities.Where(pd => pd.PostulantId == postulantId);
                    _context.PostulantDisabilities.RemoveRange(existing);

                    foreach (var disabilityId in model.DisabilityTypeIds)
                    {
                        _context.PostulantDisabilities.Add(new PostulantDisability
                        {
                            Id = Guid.NewGuid(),
                            PostulantId = postulantId,
                            DisabilityTypeId = disabilityId,
                            CreatedAt = now,
                            CreatedBy = createdBy
                        });
                    }
                }

                // 3. Duplicate check (postulante + modalidad + ventana del término activo)
                var activeTerm = await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.IsActive, ct);
                if (activeTerm != null)
                {
                    var termStartDateUtc = new DateTimeOffset(activeTerm.StartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                    var existing = await _context.Inscriptions
                        .AnyAsync(i => i.PostulantId == postulantId &&
                                       i.ModalityId == model.ModalityId &&
                                       i.CreatedAt >= termStartDateUtc, ct);
                    if (existing)
                    {
                        return new InscriptionRegisterResult
                        {
                            Outcome = InscriptionOutcome.Duplicate,
                            Message = "Ya te encuentras registrado para este proceso de admisión en esta modalidad."
                        };
                    }
                }

                // Normalizar a MAYÚSCULAS
                model.Name = model.Name.ToUpper().Trim();
                model.FatherSurname = model.FatherSurname.ToUpper().Trim();
                model.MotherSurname = model.MotherSurname.ToUpper().Trim();
                if (model.Address != null) model.Address = model.Address.ToUpper().Trim();
                if (model.OtherSchool != null) model.OtherSchool = model.OtherSchool.ToUpper().Trim();
                if (model.SchoolType != null) model.SchoolType = model.SchoolType.ToUpper().Trim();
                if (model.EducationalLevel != null) model.EducationalLevel = model.EducationalLevel.ToUpper().Trim();
                if (model.Grade != null) model.Grade = model.Grade.ToUpper().Trim();

                // 4. Inscription
                var inscriptionId = Guid.NewGuid();
                var codePostulant = await _codeService.GenerateNextAsync(model.ModalityId, model.DocumentNumber);
                var inscription = new Inscription
                {
                    Id = inscriptionId,
                    PostulantId = postulantId,
                    ModalityId = model.ModalityId,
                    TypeModalityId = model.TypeModalityId,
                    TypePostulantInscriptionId = model.TypePostulantId,
                    CareerId = model.CareerId,
                    CountryId = model.CountryId,
                    DistritId = model.UbigeoId,
                    CodePostulant = codePostulant,
                    State = AppConstants.InscripcionState.Pendiente,
                    IsAdmission = false,
                    CreatedAt = now,
                    CreatedBy = createdBy,
                    OtherSchool = model.OtherSchool,
                    SchoolId = model.IsOutsidePeru ? null : model.SchoolId,
                    DJ = model.TermsAccepted,
                    SchoolType = string.IsNullOrWhiteSpace(model.SchoolType) ? null : model.SchoolType.ToUpper().Trim(),
                    EducationalLevel = string.IsNullOrWhiteSpace(model.EducationalLevel) ? null : model.EducationalLevel.ToUpper().Trim(),
                    Grade = string.IsNullOrWhiteSpace(model.Grade) ? null : model.Grade.ToUpper().Trim(),
                    SourceUniversityId = model.SourceUniversityId,
                    SourceCareerId = model.SourceCareerId,
                    SourceCareerName = string.IsNullOrWhiteSpace(model.SourceCareerName)
                        ? null
                        : model.SourceCareerName.ToUpper().Trim()
                };
                _context.Inscriptions.Add(inscription);

                // 4b. Parent (apoderado) — sólo si el postulante es menor de edad al momento de inscribirse.
                if (IsMinor(model.BirthDate, now) && HasGuardianData(model))
                {
                    var gName = (model.GuardianName ?? string.Empty).ToUpper().Trim();
                    var gFather = (model.GuardianFatherSurname ?? string.Empty).ToUpper().Trim();
                    var gMother = (model.GuardianMotherSurname ?? string.Empty).ToUpper().Trim();

                    _context.Parents.Add(new Parent
                    {
                        Id = Guid.NewGuid(),
                        PostulantId = postulantId,
                        InscriptionId = inscriptionId,
                        Name = gName,
                        FirstNameFather = gFather,
                        FirstNameMother = gMother,
                        FullName = $"{gName} {gFather} {gMother}".Trim(),
                        TypeDocument = "DNI",
                        NumberDocument = (model.GuardianDni ?? string.Empty).Trim(),
                        Phone = (model.GuardianPhone ?? string.Empty).Trim(),
                        Email = string.IsNullOrWhiteSpace(model.GuardianEmail) ? null : model.GuardianEmail.Trim(),
                        CreatedAt = now,
                        CreatedBy = createdBy
                    });
                }

                // 4b. Validate payment data consistency
                if (!string.IsNullOrEmpty(model.PaymentCode) || model.PaymentVoucher != null)
                {
                    if (model.MethodPaymentId == null || model.MethodPaymentId == Guid.Empty)
                    {
                        return new InscriptionRegisterResult
                        {
                            Outcome = InscriptionOutcome.Error,
                            Message = "Debe seleccionar un medio de pago."
                        };
                    }

                    if (model.PaymentVoucher != null && string.IsNullOrWhiteSpace(model.PaymentCode))
                    {
                        return new InscriptionRegisterResult
                        {
                            Outcome = InscriptionOutcome.Error,
                            Message = "Debe ingresar el número de comprobante / código de operación."
                        };
                    }
                }

                // 4c. Duplicate PaymentCode check
                if (!string.IsNullOrEmpty(model.PaymentCode))
                {
                    var activeTermPay = await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.IsActive, ct);
                    if (activeTermPay != null)
                    {
                        var termStartPay = new DateTimeOffset(activeTermPay.StartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                        var codeExists = await _context.Payments
                            .AnyAsync(p => p.OperationCode == model.PaymentCode && p.CreatedAt >= termStartPay, ct);
                        if (codeExists)
                        {
                            return new InscriptionRegisterResult
                            {
                                Outcome = InscriptionOutcome.Duplicate,
                                Message = "El código de operación ingresado ya fue registrado en otra inscripción. Verifique el comprobante."
                            };
                        }
                    }
                }

                // 5. Payment voucher
                if (model.PaymentVoucher != null || !string.IsNullOrEmpty(model.PaymentCode))
                {
                    string voucherPath = string.Empty;
                    if (model.PaymentVoucher != null)
                    {
                        currentFileContext = "Comprobante de pago";
                        voucherPath = await _files.SaveFileAsync(model.PaymentVoucher, "Payments");
                    }

                    var paymentId = Guid.NewGuid();
                    var operationCode = model.PaymentCode ?? "S/C";

                    // Consultar API externa de pagos: traer todos los comprobantes del postulante,
                    // guardarlos en DB y luego buscar coincidencia con el código ingresado.
                    var externalApi = await _externalApi.FindApiByCategoryAsync("Payment", ct);
                    if (externalApi != null)
                    {
                        var anonUser = new ClaimsPrincipal(new ClaimsIdentity());
                        await _externalApi.FetchAndSavePaymentsAsync(
                            externalApi.Id, user.Document, anonUser, input.RemoteIp, ct);
                    }

                    // Auto-asociar con ExternalPaymentVoucher:
                    // 1. Normaliza el código ingresado → extrae solo la secuencia numérica (sin ceros a la izquierda)
                    //    Ej: "001-0000656" → "656", "001-6545" → "6545", "1-235" → "235", "v001234" → "1234"
                    // 2. Filtra por DNI del postulante para evitar cruces con pagos de terceros
                    // 3. Compara el valor normalizado contra cada serial de la API
                    Guid? externalVoucherId = null;
                    if (!string.IsNullOrWhiteSpace(operationCode) && operationCode != "S/C")
                    {
                        var normalizedOp = NormalizePaymentCode(operationCode);
                        if (normalizedOp != null)
                        {
                            var userVouchers = await _context.ExternalPaymentVouchers
                                .AsNoTracking()
                                .Where(v => v.UserName == user.Document)
                                .ToListAsync(ct);

                            var match = userVouchers
                                .FirstOrDefault(v => NormalizePaymentCode(v.SerialVoucher) == normalizedOp);

                            if (match != null)
                                externalVoucherId = match.Id;
                        }
                    }

                    _context.Payments.Add(new ADMISION.ENTITIES.Models.EconomicManagement.Payments
                    {
                        Id = paymentId,
                        InscriptionId = inscriptionId,
                        OperationCode = operationCode,
                        Amount = model.PaymentAmount,
                        FilePath = voucherPath,
                        MethodPaymentId = model.MethodPaymentId,
                        IsApproved = false,
                        DatePayment = now,
                        CreatedAt = now,
                        CreatedBy = createdBy,
                        ExternalPaymentVoucherId = externalVoucherId
                    });
                }

                // 5b. Profile photo
                if (model.ProfilePhoto != null)
                {
                    currentFileContext = "Foto de perfil";

                    // Desactivar fotos primarias existentes del postulante
                    var existingPhotos = await _context.PostulantPhotos
                        .Where(p => p.PostulantId == postulantId && p.IsPrimary)
                        .ToListAsync(ct);
                    foreach (var photo in existingPhotos)
                    {
                        photo.IsPrimary = false;
                    }

                    // Validar el archivo (reusa la lógica de FileService)
                    var validation = await _files.ValidateFileAsync(model.ProfilePhoto);
                    if (!validation.IsValid)
                    {
                        throw new InvalidFileException(model.ProfilePhoto.FileName ?? "foto", validation.Reason);
                    }

                    // Guardar en {BaseStoragePath}/{year}/photos/{postulantId}/{timestamp}.ext
                    var photosDir = Path.Combine(_files.GetBaseStoragePath(), now.Year.ToString(), "photos", postulantId.ToString());
                    Directory.CreateDirectory(photosDir);

                    var ext = Path.GetExtension(model.ProfilePhoto.FileName ?? ".jpg").ToLowerInvariant();
                    var fileName = $"{now.ToUnixTimeSeconds()}{ext}";
                    var absolutePath = Path.Combine(photosDir, fileName);

                    using (var stream = new FileStream(absolutePath, FileMode.Create))
                    {
                        await model.ProfilePhoto.CopyToAsync(stream);
                    }

                    var photoPath = $"uploads/{now.Year}/photos/{postulantId}/{fileName}";

                    _context.PostulantPhotos.Add(new PostulantPhoto
                    {
                        Id = Guid.NewGuid(),
                        PostulantId = postulantId,
                        PhotoUrl = photoPath,
                        IsPrimary = true,
                        CreatedAt = now,
                        CreatedBy = createdBy
                    });

                    // Sincronizar la foto activa en User.PhotoUrl
                    user.PhotoUrl = photoPath;

                    if (_context.Entry(user).State == EntityState.Unchanged)
                    {
                        _context.Users.Update(user);
                    }
                }

                // 6. Dynamic file requirements
                if (input.RequirementFiles.Any())
                {
                    var requirementNames = await _context.FileRequirementManagements.AsNoTracking()
                        .Select(r => new { r.Id, r.Name })
                        .ToDictionaryAsync(r => r.Id, r => r.Name, ct);

                    foreach (var rf in input.RequirementFiles)
                    {
                        var label = requirementNames.TryGetValue(rf.RequirementId, out var n) && !string.IsNullOrWhiteSpace(n)
                            ? $"Requisito \"{n}\""
                            : "Requisito";

                        currentFileContext = label;
                        var relativePath = await _files.SaveFileAsync(rf.File, "Requirements");

                        _context.FileSubmissions.Add(new FileSubmission
                        {
                            Id = Guid.NewGuid(),
                            InscriptionId = inscriptionId,
                            FileRequirementManagementId = rf.RequirementId,
                            FileName = rf.File.FileName,
                            FilePath = relativePath,
                            FileType = rf.File.ContentType,
                            FileSize = (rf.File.Length / 1024.0 / 1024.0).ToString("F2") + " MB",
                            CreatedAt = now,
                            CreatedBy = createdBy
                        });
                    }
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                // Notificación al panel admin (errores aquí no tumban la inscripción).
                try
                {
                    await _notifications.CreateInscriptionNotificationAsync(inscriptionId);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "No se pudo crear la notificación de nueva inscripción {InscriptionId}.", inscriptionId);
                }

                return new InscriptionRegisterResult
                {
                    Outcome = InscriptionOutcome.Success,
                    InscriptionId = inscriptionId
                };
            }
            catch (InvalidFileException fex)
            {
                _logger.LogWarning(fex, "Archivo inválido durante inscripción: {FileName} (contexto: {Context})", fex.FileName, currentFileContext);
                return new InscriptionRegisterResult
                {
                    Outcome = InscriptionOutcome.InvalidFile,
                    FileName = fex.FileName,
                    FileReason = fex.Reason,
                    FileContextLabel = currentFileContext
                };
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString("N");
                _logger.LogError(ex, "InscriptionRegister falló. CorrelationId={CorrelationId}", correlationId);
                return new InscriptionRegisterResult
                {
                    Outcome = InscriptionOutcome.Error,
                    CorrelationId = correlationId,
                    Exception = ex
                };
            }
        }

        private static bool IsMinor(DateTime birthDate, DateTimeOffset now)
        {
            var today = now.UtcDateTime.Date;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age >= 0 && age < 18;
        }

        private static bool HasGuardianData(EnrollmentViewModel model)
            => !string.IsNullOrWhiteSpace(model.GuardianName)
                || !string.IsNullOrWhiteSpace(model.GuardianFatherSurname)
                || !string.IsNullOrWhiteSpace(model.GuardianMotherSurname)
                || !string.IsNullOrWhiteSpace(model.GuardianDni)
                || !string.IsNullOrWhiteSpace(model.GuardianPhone);

        private static string? NormalizePaymentCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            // Para formato caja (XXX-YYYYYYY): extraer la secuencia después del último guion
            var hyphenIdx = code.LastIndexOf('-');
            var seq = hyphenIdx >= 0 ? code[(hyphenIdx + 1)..] : code;

            // Extraer solo dígitos
            var digits = new string([.. seq.Where(c => c is >= '0' and <= '9')]);
            if (string.IsNullOrEmpty(digits)) return null;

            // Quitar ceros a la izquierda parseando como entero
            if (long.TryParse(digits, out var num))
                return num.ToString();

            return digits;
        }
    }
}
