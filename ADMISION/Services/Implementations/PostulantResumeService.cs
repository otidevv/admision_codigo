using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Biometrics;
using ADMISION.ENTITIES.Models.Integrations;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.ENTITIES.Models.Users;
using ADMISION.Services.Interfaces;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class PostulantResumeService : IPostulantResumeService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public PostulantResumeService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        // ============ Búsqueda / Detalle ============
        public async Task<IReadOnlyList<Postulant>> SearchAsync(string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
                return Array.Empty<Postulant>();

            var term = query.Trim().ToLower();
            return await _context.Postulants
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p =>
                    p.User!.Document.ToLower().Contains(term) ||
                    (p.User.FirstNameFather + " " + p.User.FirstNameMother + " " + p.User.Name).ToLower().Contains(term))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync(ct);
        }

        public Task<Postulant?> GetByIdAsync(Guid postulantId, CancellationToken ct = default)
        {
            return _context.Postulants
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Disabilities!)
                    .ThenInclude(d => d!.DisabilityType)
                .FirstOrDefaultAsync(p => p.Id == postulantId, ct);
        }

        // ============ Secciones del resumen ============
        public Task<IReadOnlyList<Inscription>> GetInscriptionsAsync(Guid postulantId, CancellationToken ct = default)
            => LoadInscriptionsAsync(postulantId, q => q
                .Include(i => i.Career).ThenInclude(c => c!.Faculty)
                .Include(i => i.Modality).ThenInclude(m => m!.Term)
                .Include(i => i.TypeModality)
                .Include(i => i.TypePostulantInscription)
                .Include(i => i.FileSubmissions!), ct);

        public Task<IReadOnlyList<Inscription>> GetPaymentsAsync(Guid postulantId, CancellationToken ct = default)
            => LoadInscriptionsAsync(postulantId, q => q
                .Include(i => i.Payments!)
                    .ThenInclude(p => p.MethodPayment)
                .Include(i => i.Payments!)
                    .ThenInclude(p => p.ExternalPaymentVoucher!)
                        .ThenInclude(epv => epv.Payments)
                .Include(i => i.Modality).ThenInclude(m => m!.Term), ct);

        public async Task<PostulantObservationsResult> GetObservationsAsync(Guid postulantId, CancellationToken ct = default)
        {
            var inscriptions = await LoadInscriptionsAsync(postulantId, q => q
                .Include(i => i.Observations!)
                .Include(i => i.Modality).ThenInclude(m => m!.Term), ct);

            var postulant = await _context.Postulants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postulantId, ct);

            var userObservations = postulant == null
                ? Array.Empty<ADMISION.ENTITIES.Models.Users.Observations>()
                : (IReadOnlyList<ADMISION.ENTITIES.Models.Users.Observations>)await _context.UserObservations
                    .AsNoTracking()
                    .Where(o => o.UserId == postulant.UserId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync(ct);

            return new PostulantObservationsResult(inscriptions, userObservations);
        }

        public Task<IReadOnlyList<Inscription>> GetResignationsAsync(Guid postulantId, CancellationToken ct = default)
            => LoadInscriptionsAsync(postulantId, q => q
                .Include(i => i.Resignations!)
                .Include(i => i.Modality).ThenInclude(m => m!.Term), ct);

        public Task<IReadOnlyList<Inscription>> GetResultsAsync(Guid postulantId, CancellationToken ct = default)
            => LoadInscriptionsAsync(postulantId, q => q
                .Include(i => i.Career)
                .Include(i => i.Modality).ThenInclude(m => m!.Term), ct);

        public async Task<Dictionary<Guid, string>> GetTematicAreaCodesAsync(Guid postulantId, CancellationToken ct = default)
        {
            var inscriptions = await LoadInscriptionsAsync(postulantId, q => q
                .Include(i => i.Modality).ThenInclude(m => m!.Term), ct);

            var careers = inscriptions
                .Where(i => i.Modality != null)
                .Select(i => new { i.CareerId, TermId = i.Modality!.TermId })
                .Distinct()
                .ToList();

            var tacLookup = await _context.TematicAreaCareers
                .AsNoTracking()
                .Include((ADMISION.ENTITIES.Models.Modality.TematicAreaCareer t) => t.TematicArea)
                .ToListAsync(ct);
            var tacDict = tacLookup.ToDictionary(
                t => (t.CareerId, t.TermId),
                t => t.TematicArea?.Code ?? string.Empty);

            var result = new Dictionary<Guid, string>();
            foreach (var i in inscriptions)
            {
                var termId = i.Modality?.TermId ?? Guid.Empty;
                if (tacDict.TryGetValue((i.CareerId, termId), out var areaCode) && !string.IsNullOrWhiteSpace(areaCode))
                    result[i.Id] = areaCode;
            }
            return result;
        }

        public Task<IReadOnlyList<Inscription>> GetForBiometricsAsync(Guid postulantId, CancellationToken ct = default)
            => LoadInscriptionsAsync(postulantId, q => q, ct);

        public async Task<IReadOnlyList<Parent>> GetParentsAsync(Guid postulantId, CancellationToken ct = default)
        {
            return await _context.Parents
                .AsNoTracking()
                .Where(p => p.PostulantId == postulantId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<IssuedDocumentItem>> GetIssuedDocumentsAsync(Guid postulantId, CancellationToken ct = default)
        {
            return await Task.FromResult<IReadOnlyList<IssuedDocumentItem>>(Array.Empty<IssuedDocumentItem>());
        }

        // ============ Observaciones ============
        public async Task<IReadOnlyList<ObservationSearchItem>> SearchObservationsAsync(Guid postulantId, string? searchTerm, CancellationToken ct = default)
        {
            var searchLower = searchTerm?.Trim().ToLowerInvariant() ?? string.Empty;

            var postulant = await _context.Postulants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postulantId, ct);

            if (postulant == null) return Array.Empty<ObservationSearchItem>();

            var inscriptionsQuery = _context.Inscriptions
                .AsNoTracking()
                .Where(i => i.PostulantId == postulantId)
                .Include(i => i.Observations!)
                .Include(i => i.Modality).ThenInclude(m => m!.Term);

            var userObsQuery = _context.UserObservations
                .AsNoTracking()
                .Where(o => o.UserId == postulant.UserId);

            var results = new List<ObservationSearchItem>();

            foreach (var ins in await inscriptionsQuery.ToListAsync(ct))
            {
                if (ins.Observations == null) continue;
                foreach (var obs in ins.Observations)
                {
                    if (!string.IsNullOrWhiteSpace(searchLower) &&
                        !obs.Observation.ToLowerInvariant().Contains(searchLower)) continue;
                    results.Add(new ObservationSearchItem
                    {
                        Id = obs.Id,
                        Kind = "inscription",
                        Observation = obs.Observation,
                        TipoObservacion = obs.TipoObservacion,
                        CreatedAt = obs.CreatedAt,
                        UpdatedAt = obs.UpdatedAt,
                        CreatedBy = obs.CreatedBy ?? string.Empty,
                        Context = ins.Modality?.Term?.Name ?? "Sin periodo",
                        CodePostulant = ins.CodePostulant
                    });
                }
            }

            foreach (var obs in await userObsQuery.ToListAsync(ct))
            {
                if (!string.IsNullOrWhiteSpace(searchLower) &&
                    !obs.Observation.ToLowerInvariant().Contains(searchLower)) continue;
                results.Add(new ObservationSearchItem
                {
                    Id = obs.Id,
                    Kind = "user",
                    Observation = obs.Observation,
                    CreatedAt = obs.CreatedAt,
                    UpdatedAt = obs.UpdatedAt,
                    CreatedBy = obs.CreatedBy ?? string.Empty,
                    Context = "General del usuario",
                    CodePostulant = null
                });
            }

            return results.OrderByDescending(r => r.CreatedAt).ToList();
        }

        public async Task<bool> AddObservationAsync(Guid postulantId, string scope, Guid? inscriptionId, string observation, string actor, string? tipoObservacion = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(observation)) return false;

            var postulant = await _context.Postulants.FirstOrDefaultAsync(p => p.Id == postulantId, ct);
            if (postulant == null) return false;

            if (string.Equals(scope, "inscription", StringComparison.OrdinalIgnoreCase) && inscriptionId.HasValue)
            {
                _context.PostulantObservations.Add(new ADMISION.ENTITIES.Models.Postulant.Observations
                {
                    Id = Guid.NewGuid(),
                    InscriptionId = inscriptionId.Value,
                    Observation = observation.Trim(),
                    TipoObservacion = string.IsNullOrWhiteSpace(tipoObservacion) ? null : tipoObservacion.Trim(),
                    CreatedBy = actor
                });
            }
            else
            {
                _context.UserObservations.Add(new ADMISION.ENTITIES.Models.Users.Observations
                {
                    Id = Guid.NewGuid(),
                    UserId = postulant.UserId,
                    Observation = observation.Trim(),
                    CreatedBy = actor
                });
            }
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateInscriptionObservationAsync(Guid observationId, Guid postulantId, string observation, string? tipoObservacion, string actor, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(observation)) return false;

            var obs = await _context.PostulantObservations.FirstOrDefaultAsync(o => o.Id == observationId, ct);
            if (obs == null) return false;

            var belongs = await _context.Inscriptions.AnyAsync(i => i.Id == obs.InscriptionId && i.PostulantId == postulantId, ct);
            if (!belongs) return false;

            obs.Observation = observation.Trim();
            obs.TipoObservacion = string.IsNullOrWhiteSpace(tipoObservacion) ? null : tipoObservacion.Trim();
            obs.UpdatedAt = DateTimeOffset.UtcNow;
            obs.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IReadOnlyList<RequirementOption>> GetPendingRequirementsAsync(Guid inscriptionId, Guid postulantId, CancellationToken ct = default)
        {
            var inscription = await _context.Inscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inscriptionId && i.PostulantId == postulantId, ct);
            if (inscription == null) return Array.Empty<RequirementOption>();

            var submittedIds = await _context.FileSubmissions
                .AsNoTracking()
                .Where(f => f.InscriptionId == inscriptionId)
                .Select(f => f.FileRequirementManagementId)
                .ToListAsync(ct);

            var required = new List<RequirementOption>();

            // Requisitos por modalidad + tipo de modalidad (con fallback a tipo null).
            var modalityReqs = await _context.ModalityRequisites
                .AsNoTracking()
                .Where(m => m.ModalityId == inscription.ModalityId && m.TypeModalityId == inscription.TypeModalityId)
                .Include(m => m.FileRequirementManagement)
                .Select(m => m.FileRequirementManagement!)
                .ToListAsync(ct);

            if (modalityReqs.Count == 0 && inscription.TypeModalityId != null)
            {
                modalityReqs = await _context.ModalityRequisites
                    .AsNoTracking()
                    .Where(m => m.ModalityId == inscription.ModalityId && m.TypeModalityId == null)
                    .Include(m => m.FileRequirementManagement)
                    .Select(m => m.FileRequirementManagement!)
                    .ToListAsync(ct);
            }

            foreach (var r in modalityReqs)
            {
                if (r == null || r.Stage == AppConstants.RequirementStage.Entry || submittedIds.Contains(r.Id)) continue;
                required.Add(new RequirementOption(r.Id, r.Id, r.Name));
            }

            // Requisito adicional por tipo de postulante.
            if (inscription.TypePostulantInscriptionId != null)
            {
                var tpReq = await _context.TypePostulantRequisites
                    .AsNoTracking()
                    .Where(t => t.TypePostulantInscriptionId == inscription.TypePostulantInscriptionId)
                    .Include(t => t.FileRequirementManagement)
                    .Select(t => t.FileRequirementManagement)
                    .FirstOrDefaultAsync(ct);
                if (tpReq != null && !submittedIds.Contains(tpReq.Id) && required.All(r => r.Id != tpReq.Id))
                {
                    required.Add(new RequirementOption(tpReq.Id, tpReq.Id, tpReq.Name));
                }
            }

            return required;
        }

        public async Task<UploadRequirementFileResult> UploadRequirementFileAsync(Guid inscriptionId, Guid postulantId, Guid requirementId, IFormFile file, string actor, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return new UploadRequirementFileResult { ErrorMessage = "Debe seleccionar un archivo." };

            var inscription = await _context.Inscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inscriptionId && i.PostulantId == postulantId, ct);
            if (inscription == null) return new UploadRequirementFileResult { NotFound = true };

            // Si el requisito ya tiene un archivo, no se registra otro: se edita reemplazándolo.
            var exists = await _context.FileSubmissions.AsNoTracking()
                .AnyAsync(f => f.InscriptionId == inscriptionId && f.FileRequirementManagementId == requirementId, ct);
            if (exists) return new UploadRequirementFileResult { AlreadyExists = true };

            var pending = await GetPendingRequirementsAsync(inscriptionId, postulantId, ct);
            if (pending.All(r => r.Id != requirementId))
                return new UploadRequirementFileResult { NotRequired = true };

            try
            {
                var relativePath = await _files.SaveFileAsync(file, "Requirements");

                _context.FileSubmissions.Add(new FileSubmission
                {
                    Id = Guid.NewGuid(),
                    InscriptionId = inscriptionId,
                    FileRequirementManagementId = requirementId,
                    FileName = file.FileName,
                    FilePath = relativePath,
                    FileType = file.ContentType,
                    FileSize = (file.Length / 1024.0 / 1024.0).ToString("F2") + " MB",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = actor
                });
                await _context.SaveChangesAsync(ct);

                return new UploadRequirementFileResult
                {
                    Success = true,
                    NewFileName = file.FileName,
                    NewFilePath = relativePath,
                    NewFileSize = (file.Length / 1024.0 / 1024.0).ToString("F2") + " MB"
                };
            }
            catch (InvalidFileException fex)
            {
                return new UploadRequirementFileResult { ErrorMessage = fex.Message };
            }
        }

        // ============ Edición de nota (SuperAdmin) ============
        public async Task<GradeUpdateOutcome> SetInscriptionGradeAsync(
            Guid postulantId, Guid inscriptionId, decimal? gradeAdmission, bool isAdmission, string actor, CancellationToken ct = default)
        {
            var inscription = await _context.Inscriptions
                .FirstOrDefaultAsync(i => i.Id == inscriptionId && i.PostulantId == postulantId, ct);
            if (inscription == null) return GradeUpdateOutcome.NotFound;

            if (gradeAdmission.HasValue && gradeAdmission.Value < 0)
                return GradeUpdateOutcome.InvalidGrade;

            // Sin nota → no puede ser admitido.
            if (!gradeAdmission.HasValue)
            {
                inscription.GradeAdmission = null;
                inscription.IsAdmission = false;
            }
            else
            {
                inscription.GradeAdmission = gradeAdmission.Value;
                inscription.IsAdmission = isAdmission;
            }

            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);
            return GradeUpdateOutcome.Updated;
        }

        public async Task<bool> ClearInscriptionGradeAsync(Guid postulantId, Guid inscriptionId, string actor, CancellationToken ct = default)
        {
            var inscription = await _context.Inscriptions
                .FirstOrDefaultAsync(i => i.Id == inscriptionId && i.PostulantId == postulantId, ct);
            if (inscription == null) return false;

            inscription.GradeAdmission = null;
            inscription.IsAdmission = false;
            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        // ============ Fotos ============
        public async Task<PhotoCaptureResult> SavePhotoAsync(Guid postulantId, string base64Image, string actor, string photosWebRoot, CancellationToken ct = default)
        {
            var postulant = await _context.Postulants
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postulantId, ct);
            if (postulant == null) return new PhotoCaptureResult { PostulantNotFound = true, ErrorMessage = "Postulante no encontrado" };

            var year = DateTime.UtcNow.Year.ToString();
            var uploadDir = Path.Combine(photosWebRoot, postulantId.ToString());
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            var fileName = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.jpg";
            var filePath = Path.Combine(uploadDir, fileName);
            var photoUrl = $"uploads/{year}/photos/{postulantId}/{fileName}";

            try
            {
                var raw = base64Image.Contains(',') ? base64Image.Split(',')[1] : base64Image;
                var bytes = Convert.FromBase64String(raw);
                await File.WriteAllBytesAsync(filePath, bytes, ct);
            }
            catch (Exception ex)
            {
                return new PhotoCaptureResult { ErrorMessage = "Error al procesar la imagen: " + ex.Message };
            }

            // Desactivar las fotos primarias previas y registrar la nueva.
            var previous = await _context.PostulantPhotos
                .Where(p => p.PostulantId == postulantId)
                .ToListAsync(ct);
            foreach (var p in previous) p.IsPrimary = false;

            _context.PostulantPhotos.Add(new PostulantPhoto
            {
                Id = Guid.NewGuid(),
                PostulantId = postulantId,
                PhotoUrl = photoUrl,
                IsPrimary = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actor
            });

            // Sync con User.PhotoUrl para que la foto activa sea fácil de consultar.
            if (postulant.User != null) postulant.User.PhotoUrl = photoUrl;

            await _context.SaveChangesAsync(ct);
            return new PhotoCaptureResult { Success = true, PhotoUrl = photoUrl };
        }

        public async Task<IReadOnlyList<PostulantPhotoListItem>> GetPhotosAsync(Guid postulantId, CancellationToken ct = default)
        {
            return await _context.PostulantPhotos
                .AsNoTracking()
                .Where(p => p.PostulantId == postulantId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostulantPhotoListItem(p.Id, p.PhotoUrl, p.IsPrimary, p.CreatedAt))
                .ToListAsync(ct);
        }

        public async Task<bool> SetPrimaryPhotoAsync(Guid postulantId, Guid photoId, CancellationToken ct = default)
        {
            var postulant = await _context.Postulants
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postulantId, ct);
            if (postulant == null) return false;

            var photos = await _context.PostulantPhotos
                .Where(p => p.PostulantId == postulantId)
                .ToListAsync(ct);

            var target = photos.FirstOrDefault(p => p.Id == photoId);
            if (target == null) return false;

            foreach (var p in photos) p.IsPrimary = (p.Id == photoId);
            if (postulant.User != null) postulant.User.PhotoUrl = target.PhotoUrl;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeletePhotoResult> DeletePhotoAsync(Guid postulantId, Guid photoId, string baseStoragePath, CancellationToken ct = default)
        {
            var photo = await _context.PostulantPhotos
                .FirstOrDefaultAsync(p => p.Id == photoId && p.PostulantId == postulantId, ct);
            if (photo == null) return new DeletePhotoResult { NotFound = true };

            var wasPrimary = photo.IsPrimary;
            var deletedUrl = photo.PhotoUrl;

            _context.PostulantPhotos.Remove(photo);

            string? newPrimaryUrl = null;
            if (wasPrimary)
            {
                var nextPrimary = await _context.PostulantPhotos
                    .Where(p => p.PostulantId == postulantId && p.Id != photoId)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                var postulant = await _context.Postulants
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == postulantId, ct);

                if (nextPrimary != null)
                {
                    nextPrimary.IsPrimary = true;
                    if (postulant?.User != null) postulant.User.PhotoUrl = nextPrimary.PhotoUrl;
                    newPrimaryUrl = nextPrimary.PhotoUrl;
                }
                else if (postulant?.User != null)
                {
                    postulant.User.PhotoUrl = null;
                }
            }

            await _context.SaveChangesAsync(ct);

            // El archivo físico se elimina tras commit.
            try
            {
                if (!string.IsNullOrWhiteSpace(deletedUrl) && !string.IsNullOrWhiteSpace(baseStoragePath))
                {
                    var relative = deletedUrl.Replace('/', Path.DirectorySeparatorChar);
                    var uploadsPrefix = "uploads" + Path.DirectorySeparatorChar;
                    if (relative.StartsWith(uploadsPrefix))
                        relative = relative.Substring(uploadsPrefix.Length);
                    var absolute = Path.Combine(baseStoragePath, relative);
                    if (File.Exists(absolute)) File.Delete(absolute);
                }
            }
            catch { }

            return new DeletePhotoResult
            {
                Success = true,
                DeletedPrimary = wasPrimary,
                NewPrimaryPhotoUrl = newPrimaryUrl
            };
        }

        // ============ Huellas ============
        public async Task<IReadOnlyList<FingerprintListItem>> GetFingerprintsAsync(Guid postulantId, CancellationToken ct = default)
        {
            return await _context.Fingerprints
                .AsNoTracking()
                .Where(f => f.PostulantId == postulantId)
                .OrderBy(f => f.FingerIndex)
                .Select(f => new FingerprintListItem(f.Id, f.FingerIndex, f.CreatedAt, f.ImageBase64))
                .ToListAsync(ct);
        }

        public async Task<FingerprintCaptureOutcome> SaveFingerprintAsync(string actor,Guid postulantId, string template, string? imageBase64, string? deviceIp, CancellationToken ct = default)
        {
            var postulant = await _context.Postulants.FindAsync(new object[] { postulantId }, ct);
            if (postulant == null)
                return new FingerprintCaptureOutcome { PostulantNotFound = true, ErrorMessage = "Postulante no encontrado" };

            var current = await _context.Fingerprints.CountAsync(f => f.PostulantId == postulantId, ct);
            if (current >= 10)
                return new FingerprintCaptureOutcome { LimitReached = true, ErrorMessage = "El postulante ya tiene 10 huellas registradas." };

            // Próximo índice disponible (0..9).
            var used = await _context.Fingerprints
                .Where(f => f.PostulantId == postulantId)
                .Select(f => f.FingerIndex)
                .ToListAsync(ct);
            int nextIndex = 0;
            for (int i = 0; i < 10; i++)
            {
                if (!used.Contains(i)) { nextIndex = i; break; }
            }
            _context.Fingerprints.Add(new Fingerprint
            {
                Id = Guid.NewGuid(),
                PostulantId = postulantId,
                FingerIndex = nextIndex,
                Template = template,
                ImageBase64 = imageBase64,
                DeviceIp = deviceIp,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actor
            });
            await _context.SaveChangesAsync(ct);
            return new FingerprintCaptureOutcome { Success = true };
        }

        public async Task<bool> DeleteFingerprintAsync(Guid postulantId, Guid fingerprintId, CancellationToken ct = default)
        {
            var fp = await _context.Fingerprints.FirstOrDefaultAsync(
                f => f.Id == fingerprintId && f.PostulantId == postulantId, ct);
            if (fp == null) return false;

            _context.Fingerprints.Remove(fp);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        // ============ Helper compartido ============
        private async Task<IReadOnlyList<Inscription>> LoadInscriptionsAsync(
            Guid postulantId,
            Func<IQueryable<Inscription>, IQueryable<Inscription>> include,
            CancellationToken ct)
        {
            var q = _context.Inscriptions.AsNoTracking().Where(i => i.PostulantId == postulantId);
            q = include(q);
            return await q.OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        }

        // ============ Validación de archivos ============
        public async Task<PostulantValidationDto?> GetValidationAsync(Guid postulantId, CancellationToken ct = default)
        {
            var postulant = await _context.Postulants.AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postulantId, ct);
            if (postulant == null) return null;

            var inscriptions = await _context.Inscriptions.AsNoTracking()
                .Where(i => i.PostulantId == postulantId)
                .Include(i => i.Career).ThenInclude(c => c!.Faculty)
                .Include(i => i.Modality).ThenInclude(m => m!.Term)
                .Include(i => i.TypeModality)
                .Include(i => i.TypePostulantInscription)
                .Include(i => i.FileSubmissions!).ThenInclude(f => f.FileRequirementManagement)
                .Include(i => i.Payments!).ThenInclude(p => p.MethodPayment)
                .Include(i => i.Payments!).ThenInclude(p => p.ExternalPaymentVoucher!).ThenInclude(v => v.Payments)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(ct);

            var dto = new PostulantValidationDto
            {
                PostulantId = postulantId,
                FullName = postulant.User?.FullName ?? $"{postulant.User?.FirstNameFather} {postulant.User?.FirstNameMother} {postulant.User?.Name}".Trim()
            };

            foreach (var ins in inscriptions)
            {
                var group = new InscriptionValidationGroup
                {
                    InscriptionId = ins.Id,
                    CodePostulant = ins.CodePostulant,
                    State = ins.State,
                    TermName = ins.Modality?.Term?.Name,
                    ModalityName = ins.Modality?.Name,
                    TypeModalityName = ins.TypeModality?.Name,
                    CareerName = ins.Career?.Name,
                    FacultyName = ins.Career?.Faculty?.Name,
                    TypePostulantName = ins.TypePostulantInscription?.Name,
                    CreatedAt = ins.CreatedAt
                };

                // Comprobantes de pago (cualquier Payment con FilePath cuenta).
                if (ins.Payments != null)
                {
                    foreach (var pay in ins.Payments.Where(p => !string.IsNullOrWhiteSpace(p.FilePath)))
                    {
                        group.Files.Add(new ValidationFileItem
                        {
                            Id = pay.Id,
                            Kind = "payment",
                            FieldLabel = $"Comprobante de pago{(pay.MethodPayment != null ? $" — {pay.MethodPayment.Name}" : "")} (op. {pay.OperationCode})",
                            FileName = Path.GetFileName(pay.FilePath ?? string.Empty),
                            FilePath = pay.FilePath ?? string.Empty,
                            FileSize = null,
                            FileType = null,
                            IsValidated = pay.IsApproved,
                            ValidatedAt = pay.UpdatedAt,
                            ValidatedBy = pay.UpdatedBy,
                            ValidationNote = pay.Observation,
                            OperationCode = pay.OperationCode,
                            PaymentMethodName = pay.MethodPayment?.Name,
                            Amount = pay.Amount,
                            HasExternalAssociation = pay.ExternalPaymentVoucherId.HasValue,
                            ExternalVoucher = pay.ExternalPaymentVoucher
                        });
                    }
                }

                // Archivos de requisitos.
                if (ins.FileSubmissions != null)
                {
                    foreach (var fs in ins.FileSubmissions.OrderBy(f => f.FileRequirementManagement?.Name))
                    {
                        group.Files.Add(new ValidationFileItem
                        {
                            Id = fs.Id,
                            Kind = "requirement",
                            FieldLabel = fs.FileRequirementManagement?.Name ?? "Requisito sin nombre",
                            FileName = fs.FileName,
                            FilePath = fs.FilePath,
                            FileSize = fs.FileSize,
                            FileType = fs.FileType,
                            IsValidated = fs.IsValidated,
                            ValidatedAt = fs.ValidatedAt,
                            ValidatedBy = fs.ValidatedBy,
                            ValidationNote = fs.ValidationNote
                        });
                    }
                }

                dto.Inscriptions.Add(group);
            }

            return dto;
        }

        public async Task<ValidationToggleResult> SetFileValidatedAsync(Guid fileId, bool isValidated, string? note, string actor, CancellationToken ct = default)
        {
            var file = await _context.FileSubmissions.FirstOrDefaultAsync(f => f.Id == fileId, ct);
            if (file == null) return new ValidationToggleResult { Found = false };

            file.IsValidated = isValidated;
            file.ValidatedAt = isValidated ? DateTimeOffset.UtcNow : null;
            file.ValidatedBy = isValidated ? actor : null;
            file.ValidationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            file.UpdatedAt = DateTimeOffset.UtcNow;
            file.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);

            return await ReevaluateInscriptionStateAsync(file.InscriptionId, actor, ct);
        }

        public async Task<ValidationToggleResult> SetPaymentApprovedAsync(Guid paymentId, bool isApproved, string? note, string actor, CancellationToken ct = default)
        {
            var pay = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct);
            if (pay == null) return new ValidationToggleResult { Found = false };

            pay.IsApproved = isApproved;
            pay.Observation = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            pay.UpdatedAt = DateTimeOffset.UtcNow;
            pay.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);

            return await ReevaluateInscriptionStateAsync(pay.InscriptionId, actor, ct);
        }

        public async Task<ReplaceFileResult> ReplaceFileSubmissionAsync(Guid fileId, IFormFile newFile, Guid postulantId, string actor, CancellationToken ct = default)
        {
            var submission = await _context.FileSubmissions
                .Include(f => f.Inscription)
                .FirstOrDefaultAsync(f => f.Id == fileId && f.Inscription!.PostulantId == postulantId, ct);
            if (submission == null) return new ReplaceFileResult { NotFound = true };

            try { _files.DeleteFile(submission.FilePath); } catch { }

            var newPath = await _files.SaveFileAsync(newFile, "Requirements");

            submission.FilePath = newPath;
            submission.FileName = newFile.FileName;
            submission.FileType = newFile.ContentType;
            submission.FileSize = (newFile.Length / 1024.0 / 1024.0).ToString("F2") + " MB";
            submission.IsValidated = false;
            submission.ValidatedAt = null;
            submission.ValidatedBy = null;
            submission.ValidationNote = null;
            submission.UpdatedAt = DateTimeOffset.UtcNow;
            submission.UpdatedBy = actor;

            await _context.SaveChangesAsync(ct);

            return new ReplaceFileResult
            {
                Success = true,
                NewFilePath = newPath,
                NewFileName = newFile.FileName,
                NewFileSize = submission.FileSize
            };
        }

        /// <summary>
        /// Recalcula el progreso de validación de una inscripción. Cuando todos
        /// los archivos (requisitos + pagos) están aprobados y el expediente
        /// estaba en <c>Pendiente</c>, lo mueve automáticamente a <c>Aprobado</c>.
        /// El reverso (desmarcar un archivo cuando ya estaba Aprobado) lo
        /// devuelve a <c>Pendiente</c>. No toca estados terminales como
        /// Rechazado, Observado o Retirado (deben ajustarse manualmente).
        /// </summary>
        private async Task<ValidationToggleResult> ReevaluateInscriptionStateAsync(Guid inscriptionId, string actor, CancellationToken ct)
        {
            var inscription = await _context.Inscriptions.FirstOrDefaultAsync(i => i.Id == inscriptionId, ct);
            if (inscription == null)
                return new ValidationToggleResult { Found = true, InscriptionId = inscriptionId };

            var requirementTotal = await _context.FileSubmissions.AsNoTracking()
                .CountAsync(f => f.InscriptionId == inscriptionId, ct);
            var requirementOk = await _context.FileSubmissions.AsNoTracking()
                .CountAsync(f => f.InscriptionId == inscriptionId && f.IsValidated, ct);
            var paymentTotal = await _context.Payments.AsNoTracking()
                .CountAsync(p => p.InscriptionId == inscriptionId && p.FilePath != null && p.FilePath != "", ct);
            var paymentOk = await _context.Payments.AsNoTracking()
                .CountAsync(p => p.InscriptionId == inscriptionId && p.FilePath != null && p.FilePath != "" && p.IsApproved, ct);

            var total = requirementTotal + paymentTotal;
            var ok = requirementOk + paymentOk;
            var allValidated = total > 0 && ok == total;

            var previousState = inscription.State;
            string newState = previousState;

            // Solo movemos entre Pendiente ↔ Aprobado de forma automática.
            if (allValidated && previousState == AppConstants.InscripcionState.Pendiente)
            {
                newState = AppConstants.InscripcionState.Aprobado;
            }
            else if (!allValidated && previousState == AppConstants.InscripcionState.Aprobado)
            {
                newState = AppConstants.InscripcionState.Pendiente;
            }

            if (newState != previousState)
            {
                inscription.State = newState;
                inscription.UpdatedAt = DateTimeOffset.UtcNow;
                inscription.UpdatedBy = actor;
                await _context.SaveChangesAsync(ct);
            }

            return new ValidationToggleResult
            {
                Found = true,
                InscriptionId = inscriptionId,
                PreviousState = previousState,
                NewState = newState,
                ValidatedCount = ok,
                TotalCount = total
            };
        }

        // ============ Edición de datos personales ============
        public async Task<ADMISION.ENTITIES.Models.Users.Users?> GetUserForEditAsync(Guid postulantId, CancellationToken ct = default)
        {
            var postulant = await _context.Postulants
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postulantId, ct);
            return postulant?.User;
        }

        public async Task<bool> UpdatePersonalDataAsync(Guid postulantId, ADMISION.ENTITIES.Models.Users.Users updated, List<Guid>? disabilityTypeIds, string? conadisNumber, string actor, CancellationToken ct = default)
        {
            var postulant = await _context.Postulants
                .Include(p => p.User)
                .Include(p => p.Disabilities)
                .FirstOrDefaultAsync(p => p.Id == postulantId, ct);
            if (postulant?.User == null) return false;

            var u = postulant.User;
            u.Name = (updated.Name ?? "").Trim().ToUpperInvariant();
            u.FirstNameFather = (updated.FirstNameFather ?? "").Trim().ToUpperInvariant();
            u.FirstNameMother = (updated.FirstNameMother ?? "").Trim().ToUpperInvariant();
            u.FullName = $"{u.Name} {u.FirstNameFather} {u.FirstNameMother}".Trim();
            u.DocumentType = (updated.DocumentType ?? "").Trim().ToUpperInvariant();
            u.Document = (updated.Document ?? "").Trim();
            u.Email = updated.Email;
            u.PhoneNumber = updated.PhoneNumber;
            u.Address = string.IsNullOrWhiteSpace(updated.Address) ? null : updated.Address.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(updated.Genero)) u.Genero = updated.Genero;
            u.CivilStatus = string.IsNullOrWhiteSpace(updated.CivilStatus) ? null : updated.CivilStatus;
            if (updated.Birthdate != default) u.Birthdate = updated.Birthdate.ToUniversalTime();
            u.UpdatedAt = DateTimeOffset.UtcNow;
            u.UpdatedBy = actor;

            postulant.ConadisNumber = string.IsNullOrWhiteSpace(conadisNumber) ? null : conadisNumber.Trim();
            postulant.UpdatedAt = DateTimeOffset.UtcNow;
            postulant.UpdatedBy = actor;

            var existingDisabilities = _context.PostulantDisabilities.Where(pd => pd.PostulantId == postulantId);
            _context.PostulantDisabilities.RemoveRange(existingDisabilities);

            if (disabilityTypeIds != null && disabilityTypeIds.Any())
            {
                foreach (var disabilityId in disabilityTypeIds)
                {
                    _context.PostulantDisabilities.Add(new PostulantDisability
                    {
                        Id = Guid.NewGuid(),
                        PostulantId = postulantId,
                        DisabilityTypeId = disabilityId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = actor
                    });
                }
            }

            await _context.SaveChangesAsync(ct);
            return true;
        }

        // ============ Edición de inscripción desde el expediente ============
        public Task<Inscription?> GetInscriptionForEditAsync(Guid postulantId, Guid inscriptionId, CancellationToken ct = default)
        {
            return _context.Inscriptions
                .Include(i => i.Postulant).ThenInclude(p => p!.User)
                .Include(i => i.Career)
                .Include(i => i.Modality).ThenInclude(m => m!.Term)
                .Include(i => i.TypeModality)
                .Include(i => i.TypePostulantInscription)
                .Include(i => i.School).ThenInclude(s => s!.Distrit).ThenInclude(d => d!.Province).ThenInclude(p => p!.Department)
                .Include(i => i.Country)
                .Include(i => i.Distrit).ThenInclude(d => d!.Province).ThenInclude(p => p!.Department)
                .Include(i => i.Parents!)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId && i.PostulantId == postulantId, ct);
        }

        public async Task<bool> UpdateInscriptionAsync(Guid postulantId, Inscription updated, string actor, CancellationToken ct = default)
        {


            var inscription = await _context.Inscriptions
                .Include(i => i.Postulant).ThenInclude(p => p!.User)
                .Include(i => i.Parents!)
                .FirstOrDefaultAsync(i => i.Id == updated.Id && i.PostulantId == postulantId, ct);
            if (inscription == null)
            {
                return false;
            }

            // ── Campos obligatorios: siempre se actualizan ──

            inscription.State = updated.State;
            inscription.CareerId = updated.CareerId;
            inscription.ModalityId = updated.ModalityId;
            inscription.CountryId = updated.CountryId;

            // ── Campos opcionales de la inscripción: solo se escriben si llegan con valor ──
            if (updated.TypeModalityId.HasValue)
            {
                inscription.TypeModalityId = updated.TypeModalityId;
            }
            if (updated.TypePostulantInscriptionId.HasValue)
            {
                inscription.TypePostulantInscriptionId = updated.TypePostulantInscriptionId;
            }
            if (updated.DistritId.HasValue)
            {
                inscription.DistritId = updated.DistritId;
            }

            // ── Colegio: solo se actualiza si el formulario envía un SchoolId ──
            if (updated.SchoolId.HasValue)
            {
                inscription.SchoolId = updated.SchoolId;
            }
            else
            {
                Console.WriteLine($"[UpdateInscription] SchoolId: sin cambio (formulario vacío, se conserva {inscription.SchoolId})");
            }
            if (!string.IsNullOrWhiteSpace(updated.OtherSchool))
            {
                inscription.OtherSchool = updated.OtherSchool.Trim().ToUpperInvariant();
            }
            if (!string.IsNullOrWhiteSpace(updated.SchoolType))
            {
                inscription.SchoolType = updated.SchoolType.Trim().ToUpperInvariant();
            }
            if (!string.IsNullOrWhiteSpace(updated.EducationalLevel))
            {
                inscription.EducationalLevel = updated.EducationalLevel.Trim().ToUpperInvariant();
            }
            if (!string.IsNullOrWhiteSpace(updated.Grade))
            {
                inscription.Grade = updated.Grade.Trim().ToUpperInvariant();
            }

            // ── Traslado: solo si viene con datos ──
            if (updated.SourceUniversityId.HasValue)
            {
                inscription.SourceUniversityId = updated.SourceUniversityId;
            }
            if (updated.SourceCareerId.HasValue)
            {
                inscription.SourceCareerId = updated.SourceCareerId;
            }
            if (!string.IsNullOrWhiteSpace(updated.SourceCareerName))
            {
                inscription.SourceCareerName = updated.SourceCareerName.Trim().ToUpperInvariant();
            }

            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.UpdatedBy = actor;

            // ── Apoderado: solo se guarda si el formulario trae datos ──
            var incomingParent = updated.Parents?.FirstOrDefault();
            var hasIncomingParentData = incomingParent != null
                && !string.IsNullOrWhiteSpace(incomingParent.Name);

            if (hasIncomingParentData)
            {
                var existing = inscription.Parents?.FirstOrDefault();

                if (existing != null)
                {
                    existing.Name = (incomingParent!.Name ?? "").Trim().ToUpperInvariant();
                    existing.FirstNameFather = (incomingParent.FirstNameFather ?? "").Trim().ToUpperInvariant();
                    existing.FirstNameMother = (incomingParent.FirstNameMother ?? "").Trim().ToUpperInvariant();
                    existing.FullName = $"{existing.Name} {existing.FirstNameFather} {existing.FirstNameMother}".Trim();
                    existing.TypeDocument = (incomingParent.TypeDocument ?? "").Trim().ToUpperInvariant();
                    existing.NumberDocument = (incomingParent.NumberDocument ?? "").Trim();
                    existing.Phone = (incomingParent.Phone ?? "").Trim();
                    existing.Email = string.IsNullOrWhiteSpace(incomingParent.Email) ? null : incomingParent.Email.Trim();
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    existing.UpdatedBy = actor;
                }
                else
                {
                    inscription.Parents ??= new List<Parent>();
                    var newParent = new Parent
                    {
                        Id = Guid.NewGuid(),
                        PostulantId = postulantId,
                        InscriptionId = inscription.Id,
                        Name = (incomingParent.Name ?? "").Trim().ToUpperInvariant(),
                        FirstNameFather = (incomingParent.FirstNameFather ?? "").Trim().ToUpperInvariant(),
                        FirstNameMother = (incomingParent.FirstNameMother ?? "").Trim().ToUpperInvariant(),
                        FullName = $"{incomingParent.Name} {incomingParent.FirstNameFather} {incomingParent.FirstNameMother}".Trim().ToUpperInvariant(),
                        TypeDocument = (incomingParent.TypeDocument ?? "").Trim().ToUpperInvariant(),
                        NumberDocument = (incomingParent.NumberDocument ?? "").Trim(),
                        Phone = (incomingParent.Phone ?? "").Trim(),
                        Email = string.IsNullOrWhiteSpace(incomingParent.Email) ? null : incomingParent.Email.Trim(),
                        CreatedBy = actor
                    };
                    inscription.Parents.Add(newParent);
                }
            }
            else
            {
                Console.WriteLine($"[UpdateInscription] Apoderado — Sin datos en formulario, se omite guardado.");
            }

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<Guid>> GetModalityCareerIdsAsync(Guid modalityId, CancellationToken ct = default)
        {
            return await _context.ModalityCareers
                .AsNoTracking()
                .Where(mc => mc.ModalityId == modalityId)
                .Select(mc => mc.CareerId)
                .ToListAsync(ct);
        }

        public async Task<int> PropagateUbigeoAsync(Guid postulantId, Guid currentInscriptionId, Guid? countryId, Guid? distritId, string actor, CancellationToken ct = default)
        {
            var inscriptions = await _context.Inscriptions
                .Where(i => i.PostulantId == postulantId && i.Id != currentInscriptionId)
                .ToListAsync(ct);

            var updated = 0;
            foreach (var ins in inscriptions)
            {
                if (countryId.HasValue) ins.CountryId = countryId.Value;
                if (distritId.HasValue) ins.DistritId = distritId;
                ins.UpdatedAt = DateTimeOffset.UtcNow;
                ins.UpdatedBy = actor;
                updated++;
            }

            if (updated > 0)
                await _context.SaveChangesAsync(ct);

            return updated;
        }

        public async Task<EditPaymentResult> EditPaymentAsync(Guid paymentId, Guid postulantId, string? operationCode, IFormFile? newFile, Guid? externalPaymentVoucherId, bool disassociate, string actor, CancellationToken ct = default)
        {
            var payment = await _context.Payments
                .Include(p => p.Inscription)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.Inscription!.PostulantId == postulantId, ct);
            if (payment == null) return new EditPaymentResult { NotFound = true };

            if (!string.IsNullOrWhiteSpace(operationCode))
                payment.OperationCode = operationCode.Trim();

            if (newFile != null && newFile.Length > 0)
            {
                try { _files.DeleteFile(payment.FilePath); } catch { }
                var newPath = await _files.SaveFileAsync(newFile, "Payments");
                payment.FilePath = newPath;
                payment.IsApproved = false;
                payment.Observation = null;
            }

            if (disassociate)
            {
                payment.ExternalPaymentVoucherId = null;
            }
            else if (externalPaymentVoucherId.HasValue && externalPaymentVoucherId.Value != Guid.Empty)
            {
                var voucherExists = await _context.ExternalPaymentVouchers.AnyAsync(v => v.Id == externalPaymentVoucherId.Value, ct);
                if (!voucherExists)
                    return new EditPaymentResult { Success = false, ErrorMessage = "El voucher externo seleccionado no existe." };
                payment.ExternalPaymentVoucherId = externalPaymentVoucherId.Value;
            }

            payment.UpdatedAt = DateTimeOffset.UtcNow;
            payment.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);

            return new EditPaymentResult
            {
                Success = true,
                OperationCode = payment.OperationCode,
                HasExternalAssociation = payment.ExternalPaymentVoucherId.HasValue,
                NewFileName = newFile?.FileName,
                NewFilePath = payment.FilePath,
                NewFileSize = newFile != null ? (newFile.Length / 1024.0 / 1024.0).ToString("F2") + " MB" : null
            };
        }

        public async Task<IReadOnlyList<ExternalPaymentVoucher>> GetUnassociatedExternalPaymentsAsync(Guid postulantId, CancellationToken ct = default)
        {
            var dni = await _context.Postulants
                .AsNoTracking()
                .Where(p => p.Id == postulantId)
                .Select(p => p.User!.Document)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(dni))
                return new List<ExternalPaymentVoucher>();

            var linkedIds = await _context.Payments
                .AsNoTracking()
                .Where(p => p.ExternalPaymentVoucherId != null && p.Inscription!.PostulantId == postulantId)
                .Select(p => p.ExternalPaymentVoucherId!.Value)
                .ToListAsync(ct);

            return await _context.ExternalPaymentVouchers
                .AsNoTracking()
                .Include(v => v.Payments)
                .Where(v => v.UserName == dni && !linkedIds.Contains(v.Id))
                .OrderByDescending(v => v.QueriedAt)
                .ToListAsync(ct);
        }

        public async Task<SaveResignationResult> SaveResignationAsync(Guid inscriptionId, DateTimeOffset dateResignation, string description, IFormFile? file, string actor, CancellationToken ct = default)
        {
            var inscription = await _context.Inscriptions.FindAsync(new object[] { inscriptionId }, ct);
            if (inscription == null)
                return new SaveResignationResult { Success = false, Message = "Inscripción no encontrada." };

            var filePath = "";
            if (file != null && file.Length > 0)
            {
                filePath = await _files.SaveFileAsync(file, "resignations");
            }

            var resignation = new Resignation
            {
                Id = Guid.NewGuid(),
                InscriptionId = inscriptionId,
                DateResignation = dateResignation.ToUniversalTime(),
                Description = description ?? "",
                File = filePath,
                CreatedBy = actor
            };

            _context.Resignations.Add(resignation);
            await _context.SaveChangesAsync(ct);

            return new SaveResignationResult { Success = true, Message = "Renuncia registrada exitosamente." };
        }

        public async Task<IReadOnlyList<Annulment>> GetAnnulmentsAsync(Guid postulantId, CancellationToken ct = default)
        {
            return await _context.Annulments
                .AsNoTracking()
                .Where(a => a.PostulantId == postulantId)
                .OrderByDescending(a => a.StartDate)
                .ToListAsync(ct);
        }

        public async Task<SaveAnnulmentResult> SaveAnnulmentAsync(Guid postulantId, DateTimeOffset startDate, DateTimeOffset endDate, string description, IFormFile? file, string actor, CancellationToken ct = default)
        {
            if (startDate.ToUniversalTime() > endDate.ToUniversalTime())
                return new SaveAnnulmentResult { Success = false, Message = "La fecha de fin debe ser posterior o igual a la fecha de inicio." };

            if (string.IsNullOrWhiteSpace(description))
                return new SaveAnnulmentResult { Success = false, Message = "Debe indicar el motivo de la anulación." };

            var postulant = await _context.Postulants.AnyAsync(p => p.Id == postulantId, ct);
            if (!postulant)
                return new SaveAnnulmentResult { Success = false, Message = "Postulante no encontrado." };

            var filePath = "";
            if (file != null && file.Length > 0)
            {
                filePath = await _files.SaveFileAsync(file, "annulments");
            }

            var annulment = new Annulment
            {
                Id = Guid.NewGuid(),
                PostulantId = postulantId,
                StartDate = startDate.ToUniversalTime(),
                EndDate = endDate.ToUniversalTime(),
                Description = description ?? "",
                File = filePath,
                CreatedBy = actor
            };

            _context.Annulments.Add(annulment);
            await _context.SaveChangesAsync(ct);

            return new SaveAnnulmentResult { Success = true, Message = "Anulación registrada exitosamente." };
        }

        public async Task<SaveAnnulmentResult> DeleteAnnulmentAsync(Guid postulantId, Guid annulmentId, CancellationToken ct = default)
        {
            var annulment = await _context.Annulments
                .FirstOrDefaultAsync(a => a.Id == annulmentId && a.PostulantId == postulantId, ct);
            if (annulment == null)
                return new SaveAnnulmentResult { Success = false, Message = "Anulación no encontrada." };

            if (!string.IsNullOrEmpty(annulment.File))
                _files.DeleteFile(annulment.File);

            _context.Annulments.Remove(annulment);
            await _context.SaveChangesAsync(ct);

            return new SaveAnnulmentResult { Success = true, Message = "Anulación eliminada exitosamente." };
        }
    }
}
