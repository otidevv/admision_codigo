using ADMISION.ENTITIES.Models.EconomicManagement;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.ENTITIES.Models.Schools;
using ADMISION.ENTITIES.Models.System;
using ADMISION.ENTITIES.Models.Ubigeo;
using ADMISION.ENTITIES.Models.Users;
using Microsoft.EntityFrameworkCore;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.ENTITIES.Models.Infrastructure;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Notifications;

using ADMISION.ENTITIES.Models.Integrations;
using ADMISION.ENTITIES.Models.Api;

namespace ADMISION.ENTITIES.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Biometrics
        public DbSet<ADMISION.ENTITIES.Models.Biometrics.Fingerprint> Fingerprints { get; set; }
        public DbSet<ADMISION.ENTITIES.Models.Biometrics.PostulantPhoto> PostulantPhotos { get; set; }
        public DbSet<ADMISION.ENTITIES.Models.Biometrics.PostulantAttendance> PostulantAttendances { get; set; }

        // EconomicManagement
        public DbSet<PaymentCode> PaymentCodes { get; set; }
        public DbSet<PaymentCodeModality> PaymentCodesModalities { get; set; }
        public DbSet<Payments> Payments { get; set; }
        public DbSet<MethodPayment> MethodPayments { get; set; }

        // Info
        public DbSet<Banner> Banners { get; set; } // Assuming Banner is a model in Info
        public DbSet<Brochure> Brochures { get; set; } = default!;
        public DbSet<OtherFiles> OtherFiles { get; set; }
        public DbSet<Prospect> Prospects { get; set; }
        public DbSet<PublicInfo> PublicInfos { get; set; }
        public DbSet<FaqItem> FaqItems { get; set; } = default!;
        public DbSet<Sponsor> Sponsors { get; set; } = default!;
        public DbSet<Announcement> Announcements { get; set; } = default!;

        // Infrastructure
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Pavilion> Pavilions { get; set; }
        public DbSet<ExamAssignment> ExamAssignments { get; set; }
        public DbSet<ExamSchedule> ExamSchedules { get; set; }
        public DbSet<ExamScheduleRoom> ExamScheduleRooms { get; set; }

        // Exam results
        public DbSet<ExamScoreRecord> ExamScoreRecords { get; set; }
        public DbSet<CepreImportRecord> CepreImportRecords { get; set; }
        public DbSet<CepreImportVersion> CepreImportVersions { get; set; }
        public DbSet<CepreTurn> CepreTurns { get; set; }
        public DbSet<AdmissionResultImportRecord> AdmissionResultImportRecords { get; set; }
        public DbSet<CepreMatchRecord> CepreMatchRecords { get; set; }
        public DbSet<ConsolidadoIngresantesVersion> ConsolidadoIngresantesVersions { get; set; }
        public DbSet<ConsolidadoIngresantesRecord> ConsolidadoIngresantesRecords { get; set; }
        public DbSet<PostulantTypeConfig> PostulantTypeConfigs { get; set; }
        public DbSet<ScoringProfile> ScoringProfiles { get; set; }
        public DbSet<ScoringProfileRange> ScoringProfileRanges { get; set; }

        // Modality
        public DbSet<Career> Careers { get; set; }
        public DbSet<CareerImage> CareerImages { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Modality> Modalities { get; set; }
        public DbSet<ModalityCareer> ModalityCareers { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<Vacancies> Vacancies { get; set; }
        public DbSet<TypeModality> TypeModalities { get; set; }
        public DbSet<TypeModalityCareer> TypeModalityCareers { get; set; }
        public DbSet<Beneficiarie> Beneficiaries { get; set; }
        public DbSet<TematicArea> TematicAreas { get; set; }
        public DbSet<TematicAreaCareer> TematicAreaCareers { get; set; }
        public DbSet<ScheduleEvent> ScheduleEvents { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<University> Universities { get; set; }

        // Postulant
        public DbSet<FileSubmission> FileSubmissions { get; set; }
        public DbSet<Inscription> Inscriptions { get; set; }
        public DbSet<Postulant> Postulants { get; set; }
        public DbSet<TypePostulantInscription> TypePostulantInscriptions { get; set; }
        // Observations, Resignation, Parent are also in Postulant folder? Checked file list step 20: Observations.cs, Parent.cs, Resignation.cs exist
        public DbSet<Models.Postulant.Observations> PostulantObservations { get; set; }
        public DbSet<Parent> Parents { get; set; } = default!;
        public DbSet<Resignation> Resignations { get; set; } = default!;
        public DbSet<Annulment> Annulments { get; set; } = default!;
        public DbSet<DisabilityType> DisabilityTypes { get; set; } = default!;
        public DbSet<PostulantDisability> PostulantDisabilities { get; set; } = default!;

        // Requirement
        public DbSet<FileRequirementManagement> FileRequirementManagements { get; set; }
        public DbSet<ModalityRequisite> ModalityRequisites { get; set; }
        public DbSet<TypePostulantRequisite> TypePostulantRequisites { get; set; }

        // Schools
        public DbSet<Schools> Schools { get; set; }

        // System
        public DbSet<Audit> Audits { get; set; }
        public DbSet<AccessLog> AccessLogs { get; set; }
        public DbSet<Config> Configs { get; set; }
        public DbSet<ImportJob> ImportJobs { get; set; } = default!;

        // Ubigeo
        public DbSet<Country> Countries { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Distrit> Distrits { get; set; }
        public DbSet<Provincie> Provincies { get; set; }

        // Users
        public DbSet<Models.Users.Observations> UserObservations { get; set; } // Users folder also has Observations.cs
        public DbSet<Rols> Rols { get; set; }
        public DbSet<Teachers> Teachers { get; set; }
        public DbSet<UserRol> UserRols { get; set; }
        public DbSet<Users> Users { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; } = default!;
        public DbSet<NotificationView> NotificationViews { get; set; } = default!;

        // Integrations
        public DbSet<ExternalApi> ExternalApis { get; set; } = default!;
        public DbSet<ApiQueryLog> ApiQueryLogs { get; set; } = default!;
        public DbSet<ExternalAcademicInfo> ExternalAcademicInfos { get; set; } = default!;
        public DbSet<ExternalPaymentVoucher> ExternalPaymentVouchers { get; set; } = default!;
        public DbSet<ExternalPaymentDetail> ExternalPaymentDetails { get; set; } = default!;

        // Api (JWT)
        public DbSet<ApiToken> ApiTokens { get; set; } = default!;
        public DbSet<ApiRequestLog> ApiRequestLogs { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // FaqItem self-referencing hierarchy for option-based chatbot navigation
            modelBuilder.Entity<FaqItem>()
                .HasOne(f => f.Parent)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FaqItem>()
                .HasIndex(f => f.ParentId);

            // Integrations: borrado de la API restringido si tiene historial.
            modelBuilder.Entity<ApiQueryLog>()
                .HasOne(l => l.Api)
                .WithMany()
                .HasForeignKey(l => l.ApiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExternalApi>()
                .HasIndex(a => a.Name);

            modelBuilder.Entity<ApiQueryLog>()
                .HasIndex(l => l.QueriedAt);

            // Academic info (integración)
            modelBuilder.Entity<ExternalAcademicInfo>()
                .HasOne(e => e.ExternalApi)
                .WithMany()
                .HasForeignKey(e => e.ExternalApiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExternalAcademicInfo>()
                .HasOne(e => e.QueryLog)
                .WithMany()
                .HasForeignKey(e => e.QueryLogId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExternalAcademicInfo>()
                .HasIndex(e => e.Dni);

            // Payment voucher (integración)
            modelBuilder.Entity<ExternalPaymentVoucher>()
                .HasOne(e => e.ExternalApi)
                .WithMany()
                .HasForeignKey(e => e.ExternalApiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExternalPaymentVoucher>()
                .HasOne(e => e.QueryLog)
                .WithMany()
                .HasForeignKey(e => e.QueryLogId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExternalPaymentVoucher>()
                .HasIndex(e => e.UserName);

            modelBuilder.Entity<ExternalPaymentDetail>()
                .HasOne(d => d.Voucher)
                .WithMany(v => v.Payments)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            // Users & Postulant
            modelBuilder.Entity<Users>()
                .HasOne(u => u.Postulant)
                .WithOne(p => p.User)
                .HasForeignKey<Postulant>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Inscription Relationships
            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Postulant)
                .WithMany(p => p.Inscriptions)
                .HasForeignKey(i => i.PostulantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Career)
                .WithMany(c => c.Inscriptions)
                .HasForeignKey(i => i.CareerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.School)
                .WithMany(s => s.Inscriptions)
                .HasForeignKey(i => i.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Distrit)
                .WithMany(d => d.Inscriptions)
                .HasForeignKey(i => i.DistritId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Country)
                .WithMany(c => c.Inscriptions)
                .HasForeignKey(i => i.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Modality)
                .WithMany() 
                .HasForeignKey(i => i.ModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.TypeModality)
                .WithMany()
                .HasForeignKey(i => i.TypeModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.SourceUniversity)
                .WithMany()
                .HasForeignKey(i => i.SourceUniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.SourceCareer)
                .WithMany()
                .HasForeignKey(i => i.SourceCareerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Modality Relationships
            modelBuilder.Entity<Modality>(entity =>
            {
                entity.HasOne(m => m.Term)
                    .WithMany(t => t.Modalities)
                    .HasForeignKey(m => m.TermId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Ventana horaria de inscripción: por defecto el proceso corre
                // todo el día (00:00 → 23:59:59) para no cambiar el comportamiento
                // de los registros existentes.
                entity.Property(m => m.StartTime).HasDefaultValue(new TimeOnly(0, 0));
                entity.Property(m => m.EndTime).HasDefaultValue(new TimeOnly(23, 59, 59));
            });

            // ModalityCareer (many-to-many bridge)
            modelBuilder.Entity<ModalityCareer>()
                .HasOne(mc => mc.Modality)
                .WithMany(m => m.ModalityCareers)
                .HasForeignKey(mc => mc.ModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ModalityCareer>()
                .HasOne(mc => mc.Career)
                .WithMany()
                .HasForeignKey(mc => mc.CareerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ModalityCareer>()
                .HasIndex(mc => new { mc.ModalityId, mc.CareerId })
                .IsUnique();

            // TypeModalityCareer (many-to-many bridge between TypeModality and Career)
            modelBuilder.Entity<TypeModalityCareer>()
                .HasOne(tmc => tmc.TypeModality)
                .WithMany(tm => tm.TypeModalityCareers)
                .HasForeignKey(tmc => tmc.TypeModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TypeModalityCareer>()
                .HasOne(tmc => tmc.Career)
                .WithMany()
                .HasForeignKey(tmc => tmc.CareerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TypeModalityCareer>()
                .HasIndex(tmc => new { tmc.TypeModalityId, tmc.CareerId })
                .IsUnique();

            // Faculty & Career
            modelBuilder.Entity<Career>()
                .HasOne(c => c.Faculty)
                .WithMany(f => f.Careers)
                .HasForeignKey(c => c.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Career image gallery (1 — N)
            modelBuilder.Entity<CareerImage>()
                .HasOne(ci => ci.Career)
                .WithMany(c => c.Images)
                .HasForeignKey(ci => ci.CareerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CareerImage>()
                .HasIndex(ci => new { ci.CareerId, ci.DisplayOrder });

            modelBuilder.Entity<TematicArea>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<TematicAreaCareer>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<TematicAreaCareer>()
                .HasOne(x => x.TematicArea)
                .WithMany(x => x.TematicAreaCareers)
                .HasForeignKey(x => x.TematicAreaId);

            // Ubigeo
            modelBuilder.Entity<Distrit>()
                .HasOne(d => d.Province)
                .WithMany(p => p.Distrits)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Provincie>()
                .HasOne(p => p.Department)
                .WithMany(d => d.Provincies)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schools>()
                .HasOne(s => s.Distrit)
                .WithMany() // solo si Distrit NO tiene colección de Schools
                .HasForeignKey(s => s.DistritId)
                .IsRequired(false) // Esto lo hace opcional
                .OnDelete(DeleteBehavior.SetNull); // recomendado

            // User Roles
            modelBuilder.Entity<UserRol>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRols)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRol>()
                .HasOne(ur => ur.Rol)
                .WithMany(r => r.UserRols)
                .HasForeignKey(ur => ur.RolsId)
                .OnDelete(DeleteBehavior.Restrict);

            // Infrastructure
            modelBuilder.Entity<Classroom>()
                .HasOne(c => c.Pavilion)
                .WithMany(p => p.Classrooms)
                .HasForeignKey(c => c.PavilionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAssignment>()
                .HasOne(e => e.Inscription)
                .WithMany()
                .HasForeignKey(e => e.InscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAssignment>()
                .HasOne(e => e.Classroom)
                .WithMany()
                .HasForeignKey(e => e.ClassroomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAssignment>()
                .HasOne(e => e.Term)
                .WithMany()
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAssignment>()
                .HasOne(e => e.Modality)
                .WithMany()
                .HasForeignKey(e => e.ModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAssignment>()
                .HasOne(e => e.TematicArea)
                .WithMany()
                .HasForeignKey(e => e.TematicAreaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ExamAssignment>()
                .HasIndex(e => new { e.ModalityId, e.InscriptionId })
                .IsUnique();

            modelBuilder.Entity<ExamAssignment>()
                .HasOne(e => e.ExamSchedule)
                .WithMany()
                .HasForeignKey(e => e.ExamScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ExamAssignment>()
                .HasOne(e => e.Teacher)
                .WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            // ExamSchedule
            modelBuilder.Entity<ExamSchedule>()
                .HasOne(s => s.Modality)
                .WithMany()
                .HasForeignKey(s => s.ModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamSchedule>()
                .HasOne(s => s.Term)
                .WithMany()
                .HasForeignKey(s => s.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamSchedule>()
                .HasIndex(s => s.ModalityId)
                .IsUnique();

            // ExamScheduleRoom
            modelBuilder.Entity<ExamScheduleRoom>()
                .HasOne(r => r.ExamSchedule)
                .WithMany(s => s.Rooms)
                .HasForeignKey(r => r.ExamScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamScheduleRoom>()
                .HasOne(r => r.Classroom)
                .WithMany()
                .HasForeignKey(r => r.ClassroomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamScheduleRoom>()
                .HasOne(r => r.Teacher)
                .WithMany()
                .HasForeignKey(r => r.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ExamScheduleRoom>()
                .HasOne(r => r.TematicArea)
                .WithMany()
                .HasForeignKey(r => r.TematicAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamScheduleRoom>()
                .HasIndex(r => new { r.ExamScheduleId, r.ClassroomId })
                .IsUnique();

            // Observations & Teachers Relationships
            modelBuilder.Entity<Models.Users.Observations>()
                .HasOne(o => o.User)
                .WithMany(u => u.Observations)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Teachers>()
                .HasOne(t => t.User)
                .WithMany(u => u.Teachers)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Resignation>()
                .HasOne(r => r.Inscription)
                .WithMany(i => i.Resignations)
                .HasForeignKey(r => r.InscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Annulment>()
                .HasOne(a => a.Postulant)
                .WithMany(p => p.Annulments)
                .HasForeignKey(a => a.PostulantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Parent>()
                .HasOne(p => p.Inscription)
                .WithMany(i => i.Parents)
                .HasForeignKey(p => p.InscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.Postulant.Observations>()
                .HasOne(o => o.Inscription)
                .WithMany(i => i.Observations)
                .HasForeignKey(o => o.InscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Biometrics
            modelBuilder.Entity<Models.Biometrics.Fingerprint>()
                .HasOne(f => f.Postulant)
                .WithMany()
                .HasForeignKey(f => f.PostulantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Postulant Photos
            modelBuilder.Entity<ADMISION.ENTITIES.Models.Biometrics.PostulantPhoto>()
                .HasOne(p => p.Postulant)
                .WithMany()
                .HasForeignKey(p => p.PostulantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Postulant Attendance
            modelBuilder.Entity<ADMISION.ENTITIES.Models.Biometrics.PostulantAttendance>()
                .HasOne(pa => pa.Inscription)
                .WithMany()
                .HasForeignKey(pa => pa.InscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // TypePostulantInscription
            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.TypePostulantInscription)
                .WithMany(t => t.Inscriptions)
                .HasForeignKey(i => i.TypePostulantInscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique index: un postulante no puede tener el mismo código dentro de una modalidad.
            // Safety net contra race conditions en la generación de correlativos.
            modelBuilder.Entity<Inscription>()
                .HasIndex(i => new { i.ModalityId, i.CodePostulant })
                .IsUnique();

            // Vacancies Relationships
            modelBuilder.Entity<Vacancies>()
                .HasOne(v => v.Career)
                .WithMany() // Assuming Career doesn't have a direct collection of Vacancies yet or we don't need to navigate back
                .HasForeignKey(v => v.CareerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Vacancies>()
                .HasOne(v => v.TypeModality)
                .WithMany()
                .HasForeignKey(v => v.TypeModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExamResult Relationships
            modelBuilder.Entity<ExamResult>()
                .HasOne(r => r.Term)
                .WithMany()
                .HasForeignKey(r => r.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamResult>()
                .HasOne(r => r.Modality)
                .WithMany()
                .HasForeignKey(r => r.ModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ModalityRequisite>()
                .HasOne(mr => mr.TypeModality)
                .WithMany()
                .HasForeignKey(mr => mr.TypeModalityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TypePostulantRequisite>()
                .HasOne(tpr => tpr.TypePostulantInscription)
                .WithMany()
                .HasForeignKey(tpr => tpr.TypePostulantInscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TypePostulantRequisite>()
                .HasOne(tpr => tpr.FileRequirementManagement)
                .WithMany()
                .HasForeignKey(tpr => tpr.FileRequirementManagementId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentCode>()
                .HasOne(x => x.Term)
                .WithMany()
                .HasForeignKey(x => x.TermId);

            modelBuilder.Entity<PaymentCodeModality>()
                .HasOne(x => x.PaymentCode)
                .WithMany(pc => pc.PaymentCodeModalities)
                .HasForeignKey(x => x.PaymentCodeId);

            modelBuilder.Entity<PaymentCodeModality>()
                .HasOne(x => x.Modality)
                .WithMany()
                .HasForeignKey(x => x.ModalityId);

            modelBuilder.Entity<PaymentCodeModality>()
                .HasOne(x => x.TypeModality)
                .WithMany()
                .HasForeignKey(x => x.TypeModalityId);

            // Payments → ExternalPaymentVoucher (asociación opcional)
            modelBuilder.Entity<Payments>()
                .HasOne(p => p.ExternalPaymentVoucher)
                .WithMany()
                .HasForeignKey(p => p.ExternalPaymentVoucherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Payments>()
                .HasIndex(p => p.OperationCode);

            modelBuilder.Entity<Prospect>()
                .HasOne(x => x.Term)
                .WithMany()
                .HasForeignKey(x => x.TermId);

            modelBuilder.Entity<PublicInfo>()
                .HasOne(x => x.Term)
                .WithMany()
                .HasForeignKey(x => x.TermId);

            modelBuilder.Entity<PublicInfo>()
                .HasOne(x => x.Modality)
                .WithMany()
                .HasForeignKey(x => x.ModalityId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Beneficiarie>()
                .HasOne(x => x.Term)
                .WithMany()
                .HasForeignKey(x => x.TermId);

            // ScheduleEvent
            modelBuilder.Entity<ScheduleEvent>()
                .HasOne(s => s.Term)
                .WithMany()
                .HasForeignKey(s => s.TermId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScheduleEvent>()
                .HasIndex(s => new { s.TermId, s.Phase, s.DisplayOrder });

            // Disability Relationships
            modelBuilder.Entity<PostulantDisability>()
                .HasOne(pd => pd.Postulant)
                .WithMany(p => p.Disabilities)
                .HasForeignKey(pd => pd.PostulantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostulantDisability>()
                .HasOne(pd => pd.DisabilityType)
                .WithMany(dt => dt.PostulantDisabilities)
                .HasForeignKey(pd => pd.DisabilityTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Exam results
            modelBuilder.Entity<ExamScoreRecord>()
                .HasIndex(r => new { r.InscriptionId, r.TematicAreaId });

            modelBuilder.Entity<ExamScoreRecord>()
                .HasIndex(r => r.Source);

            modelBuilder.Entity<CepreImportRecord>()
                .HasIndex(r => new { r.CreatedBy, r.CreatedAt });

            modelBuilder.Entity<CepreImportRecord>()
                .HasOne(r => r.Version)
                .WithMany()
                .HasForeignKey(r => r.VersionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CepreImportVersion>()
                .HasIndex(v => new { v.TermId, v.VersionNumber })
                .IsUnique();

            modelBuilder.Entity<CepreTurn>()
                .HasOne(t => t.Term)
                .WithMany()
                .HasForeignKey(t => t.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CepreTurn>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CepreTurn>()
                .HasIndex(t => new { t.TermId, t.UserId })
                .IsUnique();

            modelBuilder.Entity<CepreMatchRecord>()
                .HasOne(r => r.Version)
                .WithMany()
                .HasForeignKey(r => r.CepreVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CepreMatchRecord>()
                .HasOne(r => r.Inscription)
                .WithMany()
                .HasForeignKey(r => r.InscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CepreMatchRecord>()
                .HasOne(r => r.ExamResult)
                .WithMany()
                .HasForeignKey(r => r.ExamResultId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CepreMatchRecord>()
                .HasIndex(r => new { r.CreatedBy, r.CreatedAt });

            modelBuilder.Entity<AdmissionResultImportRecord>()
                .HasIndex(r => new { r.CreatedBy, r.CreatedAt });

            modelBuilder.Entity<ConsolidadoIngresantesVersion>()
                .HasIndex(v => new { v.TermId, v.VersionNumber })
                .IsUnique();

            modelBuilder.Entity<ConsolidadoIngresantesVersion>()
                .HasOne(v => v.Term)
                .WithMany()
                .HasForeignKey(v => v.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConsolidadoIngresantesRecord>()
                .HasOne(r => r.Term)
                .WithMany()
                .HasForeignKey(r => r.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConsolidadoIngresantesRecord>()
                .HasOne(r => r.Version)
                .WithMany()
                .HasForeignKey(r => r.VersionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConsolidadoIngresantesRecord>()
                .HasOne(r => r.Inscription)
                .WithMany()
                .HasForeignKey(r => r.InscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ConsolidadoIngresantesRecord>()
                .HasIndex(r => new { r.CreatedBy, r.CreatedAt });

            modelBuilder.Entity<ConsolidadoIngresantesRecord>()
                .HasIndex(r => r.TermId);

            modelBuilder.Entity<ConsolidadoIngresantesRecord>()
                .HasIndex(r => r.VersionId);

            modelBuilder.Entity<PostulantTypeConfig>()
                .HasOne(c => c.Term).WithMany()
                .HasForeignKey(c => c.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostulantTypeConfig>()
                .HasOne(c => c.Career).WithMany()
                .HasForeignKey(c => c.CareerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PostulantTypeConfig>()
                .HasOne(c => c.Modality).WithMany()
                .HasForeignKey(c => c.ModalityId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PostulantTypeConfig>()
                .HasOne(c => c.TypeModality).WithMany()
                .HasForeignKey(c => c.TypeModalityId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PostulantTypeConfig>()
                .HasIndex(c => new { c.TermId, c.Index });

            // Perfiles de calificación
            modelBuilder.Entity<ScoringProfile>(entity =>
            {
                entity.Property(p => p.PuntosCorrecta).HasPrecision(10, 4);
                entity.Property(p => p.PuntosBlanco).HasPrecision(10, 4);
                entity.Property(p => p.PuntosIncorrecta).HasPrecision(10, 4);
                entity.Property(p => p.NotaMinimaIngreso).HasPrecision(10, 4);

                entity.HasOne(p => p.Term).WithMany()
                    .HasForeignKey(p => p.TermId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.Modality).WithMany()
                    .HasForeignKey(p => p.ModalityId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.TypeModality).WithMany()
                    .HasForeignKey(p => p.TypeModalityId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.Career).WithMany()
                    .HasForeignKey(p => p.CareerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(p => new { p.IsActive, p.TermId });
            });

            modelBuilder.Entity<ScoringProfileRange>(entity =>
            {
                entity.Property(r => r.PuntosCorrecta).HasPrecision(10, 4);

                entity.HasOne(r => r.Profile)
                    .WithMany(p => p.Ranges)
                    .HasForeignKey(r => r.ScoringProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.ScoringProfileId, r.DisplayOrder });
            });

            // Notifications
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.CreatedAt);
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.EntityType, n.EntityId });

            modelBuilder.Entity<NotificationView>()
                .HasOne(v => v.Notification)
                .WithMany(n => n!.Views)
                .HasForeignKey(v => v.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationView>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationView>()
                .HasIndex(v => new { v.NotificationId, v.UserId })
                .IsUnique();

            modelBuilder.Entity<NotificationView>()
                .HasIndex(v => new { v.UserId, v.ViewedAt });

            // Api (JWT)
            modelBuilder.Entity<ApiToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApiToken>()
                .HasIndex(t => t.JwtId)
                .IsUnique();

            modelBuilder.Entity<ApiToken>()
                .HasIndex(t => new { t.UserId, t.IsRevoked });

            modelBuilder.Entity<ApiRequestLog>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ApiRequestLog>()
                .HasIndex(l => l.RequestedAt);

            modelBuilder.Entity<ApiRequestLog>()
                .HasIndex(l => l.UserId);
        }
    }
}
