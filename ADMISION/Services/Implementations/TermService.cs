using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.EconomicManagement;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class TermService : ITermService
    {
        private readonly AppDbContext _context;

        public TermService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Term>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .ToListAsync(ct);
        }

        public Task<Term?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Terms.FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public async Task<Term?> GetActiveWithModalitiesAsync(CancellationToken ct = default)
        {
            // Prioriza Active; si ninguno está marcado, cae al más reciente.
            var query = _context.Terms.AsNoTracking()
                .Include(t => t.Modalities!.Where(m => m.IsActive))
                .OrderByDescending(t => t.IsActive)
                .ThenByDescending(t => t.Year)
                .ThenByDescending(t => t.Number);

            return await query.FirstOrDefaultAsync(ct);
        }

        public async Task<Term> CreateAsync(Term term, TermReplicationOptions options, string actor, CancellationToken ct = default)
        {
            term.Id = Guid.NewGuid();
            term.CreatedAt = DateTimeOffset.UtcNow;
            term.CreatedBy = actor;
            _context.Terms.Add(term);

            if (options.Enabled)
            {
                await ReplicatePreviousAsync(term.Id, options, actor, ct);
            }

            await _context.SaveChangesAsync(ct);
            return term;
        }

        public async Task<bool> UpdateAsync(Term term, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.Id == term.Id, ct);
            if (existing == null) return false;

            term.CreatedAt = existing.CreatedAt;
            term.CreatedBy = existing.CreatedBy;
            term.UpdatedAt = DateTimeOffset.UtcNow;
            term.UpdatedBy = actor;

            _context.Terms.Update(term);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var term = await _context.Terms.FindAsync(new object[] { id }, ct);
            if (term == null) return DeleteOutcome.NotFound;

            try
            {
                _context.Terms.Remove(term);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Replicación granular de configuración del término anterior.
        //  Cada bloque respeta su propia flag en TermReplicationOptions.
        //  Mantiene mappings de IDs old→new para reasignar las FKs cruzadas
        //  (TypeModality, Modality, PaymentCode).
        // ════════════════════════════════════════════════════════════════════
        private async Task ReplicatePreviousAsync(Guid newTermId, TermReplicationOptions options, string actor, CancellationToken ct)
        {
            var previous = await _context.Terms.AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .FirstOrDefaultAsync(ct);
            if (previous == null) return;

            var now = DateTimeOffset.UtcNow;

            // Mappings cruzados — se llenan a medida que se replican entidades
            // y se consumen al crear las dependencias (PaymentCodeModality
            // necesita los 3, p.ej.).
            var paymentCodeMap = new Dictionary<Guid, Guid>();
            var modalityMap = new Dictionary<Guid, Guid>();
            var typeModalityMap = new Dictionary<Guid, Guid>();

            // ───── 1. PaymentCodes ─────
            if (options.PaymentCodes)
            {
                var paymentCodes = await _context.PaymentCodes.AsNoTracking()
                    .Where(pc => pc.TermId == previous.Id).ToListAsync(ct);
                foreach (var pc in paymentCodes)
                {
                    var newId = Guid.NewGuid();
                    paymentCodeMap[pc.Id] = newId;
                    _context.PaymentCodes.Add(new PaymentCode
                    {
                        Id = newId,
                        Code = pc.Code,
                        Description = pc.Description,
                        IsActive = pc.IsActive,
                        TermId = newTermId,
                        CreatedAt = now,
                        CreatedBy = actor
                    });
                }
            }

            // ───── 2. TematicAreaCareers ─────
            if (options.TematicAreaCareers)
            {
                var tematic = await _context.TematicAreaCareers.AsNoTracking()
                    .Where(tac => tac.TermId == previous.Id).ToListAsync(ct);
                foreach (var tac in tematic)
                {
                    _context.TematicAreaCareers.Add(new TematicAreaCareer
                    {
                        Id = Guid.NewGuid(),
                        TermId = newTermId,
                        CareerId = tac.CareerId,
                        TematicAreaId = tac.TematicAreaId,
                        CreatedAt = now,
                        CreatedBy = actor
                    });
                }
            }

            // ───── 3. Modalities + cascada (TypeModality, Vacancies, ModalityRequisites) ─────
            if (options.Modalities)
            {
                var modalities = await _context.Modalities.AsNoTracking()
                    .Where(m => m.TermId == previous.Id).ToListAsync(ct);

                foreach (var m in modalities)
                {
                    var newModalityId = Guid.NewGuid();
                    modalityMap[m.Id] = newModalityId;

                    _context.Modalities.Add(new Modality
                    {
                        Id = newModalityId,
                        Name = m.Name,
                        Description = m.Description,
                        PublicSummary = m.PublicSummary,
                        IconKey = m.IconKey,
                        Badge = m.Badge,
                        DisplayOrder = m.DisplayOrder,
                        IsActive = m.IsActive,
                        RequiresProfilePhoto = m.RequiresProfilePhoto,
                        IsMockExam = m.IsMockExam,
                        RequiresSchoolType = m.RequiresSchoolType,
                        RequiresEducationalLevel = m.RequiresEducationalLevel,
                        RequiresGrade = m.RequiresGrade,
                        StartDate = m.StartDate,
                        EndDate = m.EndDate,
                        ExamDate = m.ExamDate,
                        ResultsPublicationDate = m.ResultsPublicationDate,
                        StartingCode = m.StartingCode,
                        TermId = newTermId,
                        CreatedAt = now,
                        CreatedBy = actor
                    });

                    // 3a. TypeModalities (con mapping para los hijos siguientes).
                    var typeModalities = await _context.TypeModalities.AsNoTracking()
                        .Where(tm => tm.ModalityId == m.Id).ToListAsync(ct);
                    foreach (var tm in typeModalities)
                    {
                        var newTmId = Guid.NewGuid();
                        typeModalityMap[tm.Id] = newTmId;
                        _context.TypeModalities.Add(new TypeModality
                        {
                            Id = newTmId,
                            Name = tm.Name,
                            Description = tm.Description,
                            DiscountPercentage = tm.DiscountPercentage,
                            IsActive = tm.IsActive,
                            ModalityId = newModalityId,
                            CreatedAt = now,
                            CreatedBy = actor
                        });
                    }

                    // 3b. Vacancies (Available reseteado al total).
                    var vacancies = await _context.Vacancies.AsNoTracking()
                        .Where(v => v.ModalityId == m.Id).ToListAsync(ct);
                    foreach (var v in vacancies)
                    {
                        _context.Vacancies.Add(new Vacancies
                        {
                            Id = Guid.NewGuid(),
                            ModalityId = newModalityId,
                            CareerId = v.CareerId,
                            TypeModalityId = MapId(v.TypeModalityId, typeModalityMap),
                            Quantity = v.Quantity,
                            Available = v.Quantity,
                            CreatedAt = now,
                            CreatedBy = actor
                        });
                    }

                    // 3c. ModalityRequisites.
                    var requisites = await _context.ModalityRequisites.AsNoTracking()
                        .Where(mr => mr.ModalityId == m.Id).ToListAsync(ct);
                    foreach (var mr in requisites)
                    {
                        _context.ModalityRequisites.Add(new ModalityRequisite
                        {
                            Id = Guid.NewGuid(),
                            ModalityId = newModalityId,
                            TypeModalityId = MapId(mr.TypeModalityId, typeModalityMap),
                            FileRequirementManagementId = mr.FileRequirementManagementId,
                            CreatedAt = now,
                            CreatedBy = actor
                        });
                    }

                    // 3d. ModalityCareers (asociación carrera ↔ modalidad).
                    var modalityCareers = await _context.ModalityCareers.AsNoTracking()
                        .Where(mc => mc.ModalityId == m.Id).ToListAsync(ct);
                    foreach (var mc in modalityCareers)
                    {
                        _context.ModalityCareers.Add(new ModalityCareer
                        {
                            Id = Guid.NewGuid(),
                            ModalityId = newModalityId,
                            CareerId = mc.CareerId
                        });
                    }
                }
            }

            // ───── 4. PaymentCodeModalities (junction PaymentCode × Modality × TypeModality) ─────
            // Requiere las tres tablas anteriores. Si alguno de los IDs antiguos
            // no se replicó (porque el admin desmarcó esa flag), se omite la fila.
            if (options.PaymentCodeModalities)
            {
                var assocs = await _context.PaymentCodesModalities.AsNoTracking()
                    .Include(pcm => pcm.PaymentCode)
                    .Where(pcm => pcm.PaymentCode != null && pcm.PaymentCode.TermId == previous.Id)
                    .ToListAsync(ct);

                foreach (var pcm in assocs)
                {
                    if (!paymentCodeMap.TryGetValue(pcm.PaymentCodeId, out var newPaymentCodeId))
                        continue; // PaymentCode padre no replicado

                    Guid? newModalityId = null;
                    if (pcm.ModalityId.HasValue)
                    {
                        if (!modalityMap.TryGetValue(pcm.ModalityId.Value, out var mapped))
                            continue; // Asociado a una modalidad que no se replicó
                        newModalityId = mapped;
                    }

                    Guid? newTypeModalityId = null;
                    if (pcm.TypeModalityId.HasValue)
                    {
                        if (!typeModalityMap.TryGetValue(pcm.TypeModalityId.Value, out var mappedTm))
                            continue; // Asociado a un type-modality que no se replicó
                        newTypeModalityId = mappedTm;
                    }

                    _context.PaymentCodesModalities.Add(new PaymentCodeModality
                    {
                        Id = Guid.NewGuid(),
                        PaymentCodeId = newPaymentCodeId,
                        ModalityId = newModalityId,
                        TypeModalityId = newTypeModalityId,
                        Amount = pcm.Amount, // se replica tal cual
                        IsActive = pcm.IsActive,
                        CreatedAt = now,
                        CreatedBy = actor
                    });
                }
            }

            // ───── 5. ScheduleEvents ─────
            if (options.ScheduleEvents)
            {
                var events = await _context.ScheduleEvents.AsNoTracking()
                    .Where(e => e.TermId == previous.Id).ToListAsync(ct);
                foreach (var e in events)
                {
                    _context.ScheduleEvents.Add(new ScheduleEvent
                    {
                        Id = Guid.NewGuid(),
                        TermId = newTermId,
                        Phase = e.Phase,
                        Description = e.Description,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Schedule = e.Schedule,
                        Location = e.Location,
                        DisplayOrder = e.DisplayOrder,
                        IsActive = e.IsActive,
                        CreatedAt = now,
                        CreatedBy = actor
                    });
                }
            }

            // ───── 6. PublicInfos ─────
            if (options.PublicInfos)
            {
                var infos = await _context.PublicInfos.AsNoTracking()
                    .Where(p => p.TermId == previous.Id).ToListAsync(ct);
                foreach (var p in infos)
                {
                    // Si está vinculada a una modalidad antigua que no se replicó,
                    // la info pública pierde su anclaje — se omite.
                    Guid? newModalityId = null;
                    if (p.ModalityId.HasValue)
                    {
                        if (!modalityMap.TryGetValue(p.ModalityId.Value, out var mapped)) continue;
                        newModalityId = mapped;
                    }

                    _context.PublicInfos.Add(new PublicInfo
                    {
                        Id = Guid.NewGuid(),
                        Title = p.Title,
                        Description = p.Description,
                        Url = p.Url,
                        IsActive = p.IsActive,
                        DisplayOrder = p.DisplayOrder,
                        TermId = newTermId,
                        ModalityId = newModalityId,
                        CreatedAt = now,
                        CreatedBy = actor
                    });
                }
            }

            // ───── 7. Beneficiaries ─────
            if (options.Beneficiaries)
            {
                var beneficiaries = await _context.Beneficiaries.AsNoTracking()
                    .Where(b => b.TermId == previous.Id).ToListAsync(ct);
                foreach (var b in beneficiaries)
                {
                    _context.Beneficiaries.Add(new Beneficiarie
                    {
                        Id = Guid.NewGuid(),
                        Name = b.Name,
                        Description = b.Description,
                        IsActive = b.IsActive,
                        PercentageDiscount = b.PercentageDiscount,
                        TermId = newTermId,
                        CreatedAt = now,
                        CreatedBy = actor
                    });
                }
            }
        }

        private static Guid? MapId(Guid? oldId, Dictionary<Guid, Guid> mapping)
        {
            if (!oldId.HasValue) return null;
            return mapping.TryGetValue(oldId.Value, out var mapped) ? mapped : null;
        }

        // Proyección plana para evitar tipos anónimos en variables condicionales
        // (ToListAsync vs lista vacía) durante el cálculo del checklist.
        private class PaymentCodeModalityLite
        {
            public Guid? ModalityId { get; set; }
            public Guid? TypeModalityId { get; set; }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Checklist de configuración. Recorre las tablas que cuelgan de Term
        //  (directa o transitivamente vía Modality) y arma el reporte de qué
        //  está listo y qué falta para poder habilitar la inscripción de
        //  postulantes.
        //
        //  Estrategia: pre-cargamos los IDs por consultas baratas (count/any)
        //  y luego comparamos cobertura para detectar huecos (p. ej. modalidades
        //  sin vacantes, sin requisitos, sin precio).
        // ════════════════════════════════════════════════════════════════════
        public async Task<TermConfigChecklistDto?> GetConfigChecklistAsync(Guid termId, CancellationToken ct = default)
        {
            var term = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == termId, ct);
            if (term == null) return null;

            // IDs de modalidades del periodo (activas, no simulacro) — base para varios checks.
            var modalityIds = await _context.Modalities.AsNoTracking()
                .Where(m => m.TermId == termId && m.IsActive && !m.IsMockExam)
                .Select(m => m.Id)
                .ToListAsync(ct);
            var modalityCount = modalityIds.Count;

            // 1. Modalidades activas
            var hasModalities = modalityCount > 0;

            // 2. Tipos de modalidad (informativo — no todas las modalidades los requieren)
            var typeModalityCount = modalityCount == 0
                ? 0
                : await _context.TypeModalities.AsNoTracking()
                    .CountAsync(tm => modalityIds.Contains(tm.ModalityId) && tm.IsActive, ct);

            // 3. Fechas de examen — modalidades activas (no simulacro) con ExamDate
            var modalitiesWithExamDate = await _context.Modalities.AsNoTracking()
                .CountAsync(m => m.TermId == termId && m.IsActive && !m.IsMockExam && m.ExamDate != null, ct);
            var examDatesMissing = modalityCount - modalitiesWithExamDate;

            // 4. Áreas temáticas asignadas a carreras (por periodo)
            var tematicAreaCareerCount = await _context.TematicAreaCareers.AsNoTracking()
                .CountAsync(tac => tac.TermId == termId, ct);
            var distinctTematicAreas = await _context.TematicAreaCareers.AsNoTracking()
                .Where(tac => tac.TermId == termId)
                .Select(tac => tac.TematicAreaId)
                .Distinct()
                .CountAsync(ct);

            // 5. Vacantes — total + cobertura por modalidad activa
            var vacancyTotal = modalityCount == 0
                ? 0
                : await _context.Vacancies.AsNoTracking()
                    .CountAsync(v => modalityIds.Contains(v.ModalityId), ct);
            var modalitiesWithVacancy = modalityCount == 0
                ? 0
                : await _context.Vacancies.AsNoTracking()
                    .Where(v => modalityIds.Contains(v.ModalityId))
                    .Select(v => v.ModalityId)
                    .Distinct()
                    .CountAsync(ct);
            var modalitiesWithoutVacancy = modalityCount - modalitiesWithVacancy;

            // 6. Códigos de pago del periodo
            var paymentCodeCount = await _context.PaymentCodes.AsNoTracking()
                .CountAsync(pc => pc.TermId == termId && pc.IsActive, ct);

            // 7. PaymentCodeModality — cada modalidad activa debería tener al
            //    menos un PaymentCodeModality que la referencie (directo o por
            //    TypeModality). Verificamos cobertura.
            var paymentCodeIdsThisTerm = await _context.PaymentCodes.AsNoTracking()
                .Where(pc => pc.TermId == termId)
                .Select(pc => pc.Id)
                .ToListAsync(ct);

            var typeModalityIds = modalityCount == 0
                ? new List<Guid>()
                : await _context.TypeModalities.AsNoTracking()
                    .Where(tm => modalityIds.Contains(tm.ModalityId))
                    .Select(tm => tm.Id)
                    .ToListAsync(ct);

            var pcmRows = paymentCodeIdsThisTerm.Count == 0
                ? new List<PaymentCodeModalityLite>()
                : await _context.PaymentCodesModalities.AsNoTracking()
                    .Where(pcm => paymentCodeIdsThisTerm.Contains(pcm.PaymentCodeId) && pcm.IsActive)
                    .Select(pcm => new PaymentCodeModalityLite { ModalityId = pcm.ModalityId, TypeModalityId = pcm.TypeModalityId })
                    .ToListAsync(ct);

            // Una modalidad está "con precio" si hay un PCM directo apuntándola
            // o uno apuntando a uno de sus TypeModalities.
            var pcmDirectModalityIds = pcmRows
                .Where(r => r.ModalityId.HasValue)
                .Select(r => r.ModalityId!.Value)
                .ToHashSet();
            var pcmViaTypeModalityIds = pcmRows
                .Where(r => r.TypeModalityId.HasValue)
                .Select(r => r.TypeModalityId!.Value)
                .ToHashSet();

            var typeModalityToModality = modalityCount == 0
                ? new Dictionary<Guid, Guid>()
                : await _context.TypeModalities.AsNoTracking()
                    .Where(tm => modalityIds.Contains(tm.ModalityId))
                    .ToDictionaryAsync(tm => tm.Id, tm => tm.ModalityId, ct);

            var modalitiesPricedViaType = pcmViaTypeModalityIds
                .Where(tmId => typeModalityToModality.ContainsKey(tmId))
                .Select(tmId => typeModalityToModality[tmId])
                .ToHashSet();

            var modalitiesWithPrice = pcmDirectModalityIds
                .Concat(modalitiesPricedViaType)
                .Where(id => modalityIds.Contains(id))
                .Distinct()
                .Count();
            var modalitiesWithoutPrice = modalityCount - modalitiesWithPrice;

            // 8. Requisitos por modalidad — cobertura
            var modalityRequisiteRows = modalityCount == 0
                ? new List<Guid>()
                : await _context.ModalityRequisites.AsNoTracking()
                    .Where(mr => modalityIds.Contains(mr.ModalityId))
                    .Select(mr => mr.ModalityId)
                    .Distinct()
                    .ToListAsync(ct);
            var modalitiesWithRequisites = modalityRequisiteRows.Count;
            var modalitiesWithoutRequisites = modalityCount - modalitiesWithRequisites;

            // 9. Requisitos por tipo de postulante (global — no depende del Term)
            var typePostulantWithReqs = await _context.TypePostulantRequisites.AsNoTracking()
                .Select(tpr => tpr.TypePostulantInscriptionId)
                .Distinct()
                .CountAsync(ct);
            var typePostulantTotal = await _context.TypePostulantInscriptions.AsNoTracking()
                .CountAsync(tpi => tpi.IsActive, ct);

            // 10. Eventos del cronograma
            var scheduleEventCount = await _context.ScheduleEvents.AsNoTracking()
                .CountAsync(e => e.TermId == termId && e.IsActive, ct);

            // ── Construcción del checklist ─────────────────────────────────
            var items = new List<TermConfigChecklistItem>
            {
                new()
                {
                    Key = "modalities",
                    Label = "Modalidades del periodo",
                    Hint = "Al menos una modalidad activa asignada al periodo.",
                    Icon = "fa-layer-group",
                    Done = hasModalities,
                    Count = modalityCount,
                    Severity = "danger",
                    Href = "/admin/exam-management/modalities",
                    PendingDetail = hasModalities ? null : "Crea una modalidad para este periodo."
                },
                new()
                {
                    Key = "exam-dates",
                    Label = "Fechas de examen",
                    Hint = "Cada modalidad activa debe tener su fecha de examen asignada.",
                    Icon = "fa-calendar-day",
                    Done = hasModalities && examDatesMissing == 0,
                    Count = modalitiesWithExamDate,
                    Severity = "danger",
                    Href = "/admin/exam-management/modalities",
                    PendingDetail = !hasModalities
                        ? "Sin modalidades configuradas."
                        : examDatesMissing > 0
                            ? $"{examDatesMissing} modalidad(es) sin fecha de examen."
                            : null
                },
                new()
                {
                    Key = "type-modalities",
                    Label = "Tipos de modalidad",
                    Hint = "Sub-tipos como Ordinario A, Ordinario B, etc. (opcional por modalidad).",
                    Icon = "fa-sitemap",
                    Done = typeModalityCount > 0,
                    Count = typeModalityCount,
                    Severity = "warn",
                    Href = "/admin/exam-management/modality-types",
                    PendingDetail = typeModalityCount == 0 ? "No hay tipos de modalidad registrados." : null
                },
                new()
                {
                    Key = "tematic-areas",
                    Label = "Áreas temáticas con carreras",
                    Hint = "Carreras agrupadas en áreas temáticas para el sorteo y los exámenes.",
                    Icon = "fa-bullseye",
                    Done = tematicAreaCareerCount > 0,
                    Count = distinctTematicAreas,
                    Severity = "danger",
                    Href = "/admin/exam-management/tematic-areas",
                    PendingDetail = tematicAreaCareerCount == 0
                        ? "No hay carreras asignadas a áreas temáticas para este periodo."
                        : null
                },
                new()
                {
                    Key = "vacancies",
                    Label = "Vacantes por carrera",
                    Hint = "Cada modalidad activa debe tener vacantes registradas por carrera.",
                    Icon = "fa-chair",
                    Done = hasModalities && modalitiesWithoutVacancy == 0,
                    Count = vacancyTotal,
                    Severity = "danger",
                    Href = "/admin/exam-management/vacancies",
                    PendingDetail = !hasModalities
                        ? "Sin modalidades configuradas."
                        : modalitiesWithoutVacancy > 0
                            ? $"{modalitiesWithoutVacancy} modalidad(es) sin vacantes."
                            : null
                },
                new()
                {
                    Key = "payment-codes",
                    Label = "Códigos de pago del periodo",
                    Hint = "Conceptos de pago (matrícula, derecho de examen, etc.) registrados para el periodo.",
                    Icon = "fa-barcode",
                    Done = paymentCodeCount > 0,
                    Count = paymentCodeCount,
                    Severity = "danger",
                    Href = "/admin/economic-management/payment-codes",
                    PendingDetail = paymentCodeCount == 0
                        ? "No hay códigos de pago para este periodo."
                        : null
                },
                new()
                {
                    Key = "payment-code-modalities",
                    Label = "Precios por modalidad",
                    Hint = "Cada modalidad (o tipo de modalidad) debe tener un código de pago asociado con monto.",
                    Icon = "fa-money-bill-wave",
                    Done = hasModalities && paymentCodeCount > 0 && modalitiesWithoutPrice == 0,
                    Count = modalitiesWithPrice,
                    Severity = "danger",
                    Href = "/admin/economic-management/payment-codes",
                    PendingDetail = !hasModalities
                        ? "Sin modalidades configuradas."
                        : paymentCodeCount == 0
                            ? "Crea primero los códigos de pago."
                            : modalitiesWithoutPrice > 0
                                ? $"{modalitiesWithoutPrice} modalidad(es) sin precio asociado."
                                : null
                },
                new()
                {
                    Key = "modality-requisites",
                    Label = "Requisitos por modalidad",
                    Hint = "Documentos obligatorios que cada modalidad debe pedir al postulante.",
                    Icon = "fa-file-circle-check",
                    Done = hasModalities && modalitiesWithoutRequisites == 0,
                    Count = modalitiesWithRequisites,
                    Severity = "warn",
                    Href = "/admin/exam-management/requirements-by-modality",
                    PendingDetail = !hasModalities
                        ? "Sin modalidades configuradas."
                        : modalitiesWithoutRequisites > 0
                            ? $"{modalitiesWithoutRequisites} modalidad(es) sin requisitos."
                            : null
                },
                new()
                {
                    Key = "type-postulant-requisites",
                    Label = "Requisitos por tipo de postulante",
                    Hint = "Documentos específicos según el perfil del postulante (egresado, transferencia, etc.).",
                    Icon = "fa-id-card-clip",
                    Done = typePostulantTotal > 0 && typePostulantWithReqs == typePostulantTotal,
                    Count = typePostulantWithReqs,
                    Severity = "warn",
                    Href = "/admin/exam-management/requirements-by-type-postulant",
                    PendingDetail = typePostulantTotal == 0
                        ? "No hay tipos de postulante registrados."
                        : typePostulantWithReqs < typePostulantTotal
                            ? $"{typePostulantTotal - typePostulantWithReqs} tipo(s) de postulante sin requisitos."
                            : null
                },
                new()
                {
                    Key = "schedule-events",
                    Label = "Cronograma del proceso",
                    Hint = "Hitos visibles al postulante (inicio inscripciones, examen, resultados, matrícula).",
                    Icon = "fa-calendar-check",
                    Done = scheduleEventCount > 0,
                    Count = scheduleEventCount,
                    Severity = "warn",
                    Href = "/admin/info/cronograma",
                    PendingDetail = scheduleEventCount == 0
                        ? "Aún no se han publicado eventos del cronograma."
                        : null
                }
            };

            return new TermConfigChecklistDto
            {
                TermId = term.Id,
                TermName = term.Name,
                TermYear = term.Year,
                TermIsActive = term.IsActive,
                Items = items
            };
        }
    }
}
