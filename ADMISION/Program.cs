using System.Text;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Infrastructure.Hangfire;
using ADMISION.Services.Interceptors;
using ADMISION.Services.Background;
using ADMISION.Middleware;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

namespace admision
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // MVC + Razor Pages con antiforgery global en métodos no seguros
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });
            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.ConfigureFilter(new AutoValidateAntiforgeryTokenAttribute());
            });
            builder.Services.AddHttpClient();

            // Endurecer antiforgery manteniendo el header default (RequestVerificationToken)
            // para compatibilidad con el JS existente. En Development usamos
            // SameAsRequest para permitir HTTP local; en producción siempre HTTPS.
            var isDev = builder.Environment.IsDevelopment();
            var cookieSecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            // "__Host-" prefix requires Secure=true, así que usamos otro nombre en dev.
            var antiforgeryCookieName = isDev ? "Admision.Antiforgery" : "__Host-Antiforgery";
            var authCookieName = isDev ? "Admision.Auth" : "__Host-Admision.Auth";

            // Data Protection: persistir las llaves a disco y nombrar la app, para que
            // las cookies de auth/antiforgery sobrevivan a reinicios del App Pool y sean
            // consistentes entre instancias. Sin esto, al hostear bajo IIS las llaves se
            // generan en memoria y cualquier reciclo invalida sesiones (POST → 302 /login).
            var keysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
            Directory.CreateDirectory(keysPath);
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName("ADMISION");

            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = antiforgeryCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = cookieSecurePolicy;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.HeaderName = "RequestVerificationToken";
            });

            // Servicios de aplicación
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IBannerService, ADMISION.Services.Implementations.BannerService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IExamService, ADMISION.Services.Implementations.ExamService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IFingerprintService, ADMISION.Services.Implementations.FingerprintService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IPasswordHasher, ADMISION.Services.Implementations.PasswordHasher>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IConfigService, ADMISION.Services.Implementations.ConfigService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IFileService, ADMISION.Services.Implementations.FileService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IExamAssignmentService, ADMISION.Services.Implementations.ExamAssignmentService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IExamScheduleService, ADMISION.Services.Implementations.ExamScheduleService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IExamProcessingService, ADMISION.Services.Implementations.ExamProcessingService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IExamResultImportService, ADMISION.Services.Implementations.ExamResultImportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IPostulantCodeService, ADMISION.Services.Implementations.PostulantCodeService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.INotificationService, ADMISION.Services.Implementations.NotificationService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IDocumentService, ADMISION.Services.Implementations.DocumentService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IEmailService, ADMISION.Services.Implementations.EmailService>();
            // Renderer QuestPDF de Constancia de Ingreso — sin estado, podría ser singleton,
            // pero lo dejamos Scoped por consistencia con el resto de servicios de PDF.
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IConstanciaIngresoPdfRenderer, ADMISION.Services.Implementations.ConstanciaIngresoPdfRenderer>();

            // Servicios de consulta/CRUD de dominio (centralizan queries de los controladores).
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IUbigeoService, ADMISION.Services.Implementations.UbigeoService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ICatalogService, ADMISION.Services.Implementations.CatalogService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IFacultyService, ADMISION.Services.Implementations.FacultyService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ICareerService, ADMISION.Services.Implementations.CareerService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ISchoolService, ADMISION.Services.Implementations.SchoolService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IModalityService, ADMISION.Services.Implementations.ModalityService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IVacancyService, ADMISION.Services.Implementations.VacancyService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ITypeModalityService, ADMISION.Services.Implementations.TypeModalityService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IPaymentCodeService, ADMISION.Services.Implementations.PaymentCodeService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IUserManagementService, ADMISION.Services.Implementations.UserManagementService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ITeacherService, ADMISION.Services.Implementations.TeacherService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IPostulantQueryService, ADMISION.Services.Implementations.PostulantQueryService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ITematicAreaService, ADMISION.Services.Implementations.TematicAreaService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ITermService, ADMISION.Services.Implementations.TermService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IProspectService, ADMISION.Services.Implementations.ProspectService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IOtherFilesService, ADMISION.Services.Implementations.OtherFilesService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IBrochureService, ADMISION.Services.Implementations.BrochureService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IPublicInfoService, ADMISION.Services.Implementations.PublicInfoService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ISponsorService, ADMISION.Services.Implementations.SponsorService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IAnnouncementService, ADMISION.Services.Implementations.AnnouncementService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IScheduleEventService, ADMISION.Services.Implementations.ScheduleEventService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IBeneficiaryService, ADMISION.Services.Implementations.BeneficiaryService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IDisabilityTypeService, ADMISION.Services.Implementations.DisabilityTypeService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IFaqService, ADMISION.Services.Implementations.FaqService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IInscriptionDocumentService, ADMISION.Services.Implementations.InscriptionDocumentService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ITypePostulantInscriptionService, ADMISION.Services.Implementations.TypePostulantInscriptionService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IFileRequirementService, ADMISION.Services.Implementations.FileRequirementService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ITypePostulantRequisiteService, ADMISION.Services.Implementations.TypePostulantRequisiteService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IModalityRequisiteService, ADMISION.Services.Implementations.ModalityRequisiteService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IScoringProfileService, ADMISION.Services.Implementations.ScoringProfileService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IExamResultService, ADMISION.Services.Implementations.ExamResultService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IClassroomService, ADMISION.Services.Implementations.ClassroomService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IDocumentIssuanceService, ADMISION.Services.Implementations.DocumentIssuanceService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IProfileService, ADMISION.Services.Implementations.ProfileService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IDashboardService, ADMISION.Services.Implementations.DashboardService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ITematicAreaReportService, ADMISION.Services.Implementations.TematicAreaReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IGeneralReportService, ADMISION.Services.Implementations.GeneralReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IEconomicReportService, ADMISION.Services.Implementations.EconomicReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IVacantesReportService, ADMISION.Services.Implementations.VacantesReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IIngresantesReportService, ADMISION.Services.Implementations.IngresantesReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ISiriesReportService, ADMISION.Services.Implementations.SiriesReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ICepreReportService, ADMISION.Services.Implementations.CepreReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IResultadosReportService, ADMISION.Services.Implementations.ResultadosReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.ISorteoAulasReportService, ADMISION.Services.Implementations.SorteoAulasReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IAttendanceReportService, ADMISION.Services.Implementations.AttendanceReportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IReportExportService, ADMISION.Services.Implementations.ReportExportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IPostulantResumeService, ADMISION.Services.Implementations.PostulantResumeService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IImportJobService, ADMISION.Services.Implementations.ImportJobService>();
            // AttendanceService es typed-HttpClient porque consume BiometricBridge vía HTTP local.
            builder.Services.AddHttpClient<ADMISION.Services.Interfaces.IAttendanceService, ADMISION.Services.Implementations.AttendanceService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IPublicPortalService, ADMISION.Services.Implementations.PublicPortalService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IInscriptionLookupService, ADMISION.Services.Implementations.InscriptionLookupService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IInscriptionService, ADMISION.Services.Implementations.InscriptionService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IPostulantImportService, ADMISION.Services.Implementations.PostulantImportService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IConsolidadoConfigService, ADMISION.Services.Implementations.ConsolidadoConfigService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IConsolidadoService, ADMISION.Services.Implementations.ConsolidadoService>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IConsolidadoConsultaService, ADMISION.Services.Implementations.ConsolidadoConsultaService>();

            // Captcha (Cloudflare Turnstile / Google reCAPTCHA según Captcha:Provider).
            // Tipado a HttpClient para reaprovechar el pool del IHttpClientFactory.
            builder.Services.AddHttpClient<ADMISION.Services.Interfaces.ICaptchaService, ADMISION.Services.Implementations.CaptchaService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(8);
            });

            // Integraciones externas: invocador centralizado de APIs registradas en Admin.
            builder.Services.AddHttpClient<ADMISION.Services.Interfaces.IExternalApiService, ADMISION.Services.Implementations.ExternalApiService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options => options.IdleTimeout = TimeSpan.FromHours(2));

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddScoped<AuditInterceptor>();

            // SignalR — notificaciones en tiempo real hacia el panel admin
            builder.Services.AddSignalR();

            // Jobs
            builder.Services.AddScoped<ADMISION.Services.Background.ModalityStatusJob>();
            builder.Services.AddScoped<ADMISION.Services.Background.PostulantImportJob>();

            // Hangfire
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
            builder.Services.AddHangfireServer();

            // DbContext con interceptor de auditoría
            builder.Services.AddDbContext<ADMISION.ENTITIES.Data.AppDbContext>((sp, options) =>
            {
                var interceptor = sp.GetService<AuditInterceptor>();
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
                       .AddInterceptors(interceptor!);
            });

            // JWT config
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var jwtSecretKey = Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!);
            var jwtIssuer = jwtSection["Issuer"]!;
            var jwtAudience = jwtSection["Audience"]!;

            // Autenticación por cookies endurecidas + JWT Bearer
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/admin/restringido";
                    // Sesión por defecto: 2 horas. SlidingExpiration renueva el cookie
                    // automáticamente al pasar el 50% del tiempo en cualquier request.
                    // El JS del layout además llama a /admin/session/ping para renovar
                    // explícitamente cuando el usuario sigue interactuando.
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = authCookieName;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = cookieSecurePolicy;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        var tokenVersionClaim = context.Principal?.FindFirst("token_version")?.Value;
                        if (string.IsNullOrEmpty(tokenVersionClaim) || !int.TryParse(tokenVersionClaim, out var cookieVersion))
                        {
                            return;
                        }

                        var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!Guid.TryParse(userId, out var userGuid))
                        {
                            context.RejectPrincipal();
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var currentVersion = await db.Users.AsNoTracking()
                            .Where(u => u.Id == userGuid)
                            .Select(u => (int?)u.TokenVersion)
                            .FirstOrDefaultAsync();

                        if (currentVersion == null || currentVersion.Value != cookieVersion)
                        {
                            context.RejectPrincipal();
                        }
                    };
                })
                .AddJwtBearer("ApiBearer", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(jwtSecretKey),
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async ctx =>
                        {
                            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                            var userId = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            var jti = ctx.Principal?.FindFirst("jti")?.Value;
                            var tokenVersionClaim = ctx.Principal?.FindFirst("token_version")?.Value;

                            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jti) || !Guid.TryParse(userId, out var userGuid))
                            {
                                ctx.Fail("Token inválido: claims insuficientes.");
                                return;
                            }

                            var user = await db.Users.AsNoTracking()
                                .FirstOrDefaultAsync(u => u.Id == userGuid);

                            if (user == null || user.IsDisabled != AppConstants.Usuarios.Activo)
                            {
                                ctx.Fail("Usuario no encontrado o deshabilitado.");
                                return;
                            }

                            if (tokenVersionClaim != null
                                && int.TryParse(tokenVersionClaim, out var tokenVersion)
                                && tokenVersion != user.TokenVersion)
                            {
                                ctx.Fail("Token revocado. El usuario fue deshabilitado desde la emisión de este token.");
                                return;
                            }

                            var storedToken = await db.ApiTokens
                                .AsNoTracking()
                                .FirstOrDefaultAsync(t => t.JwtId == jti && !t.IsRevoked);

                            if (storedToken == null)
                            {
                                ctx.Fail("Token revocado o no encontrado.");
                                return;
                            }
                        }
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiConsumer", policy =>
                    policy.RequireRole(AppConstants.Roles.ApiConsumer)
                           .AddAuthenticationSchemes("ApiBearer"));
            });

            // Middleware de logging para requests API
            builder.Services.AddScoped<ApiLoggingMiddleware>();
            builder.Services.AddScoped<ADMISION.Services.Interfaces.IApiLogService, ADMISION.Services.Implementations.ApiLogService>();

            // Rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("login", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.AddPolicy("public-post", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.AddPolicy("public-lookup", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.AddPolicy("api", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: (httpContext.User?.Identity?.Name)
                            ?? httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));
            });

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            if(builder.Environment.IsDevelopment())
            {
                // En producción, habilitamos la compresión de PDFs generados para reducir el uso de ancho de banda.
                QuestPDF.Settings.EnableDebugging = true;
            }

            var app = builder.Build();

            // Seed DB
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ADMISION.ENTITIES.Data.AppDbContext>();
                    var hasher = services.GetRequiredService<ADMISION.Services.Interfaces.IPasswordHasher>();
                    ADMISION.Data.DbInitializer.Initialize(context, hasher, builder.Configuration, services.GetRequiredService<ILogger<Program>>());
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the DB.");
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.MapStaticAssets();

            // Servir los archivos subidos bajo el prefijo "/uploads".
            // Resolvemos la carpeta con la misma lógica que FileService.GetBaseStoragePath:
            //   • Si la config está en blanco  → "{WebRoot}/uploads" (default).
            //   • Si es un path absoluto       → se usa tal cual.
            //   • Si es relativo               → se resuelve contra ContentRoot.
            // Si la carpeta resuelta ya está bajo wwwroot, el proveedor de estáticos
            // default ya la sirve; registrar un segundo proveedor es redundante pero
            // inocuo. Si la carpeta es externa a wwwroot, este proveedor es necesario.
            var configuredUploadsPath = builder.Configuration["FileUpload:BaseStoragePath"];
            string uploadsPath;
            if (string.IsNullOrWhiteSpace(configuredUploadsPath))
            {
                var webRoot = string.IsNullOrEmpty(app.Environment.WebRootPath)
                    ? Path.Combine(app.Environment.ContentRootPath, "wwwroot")
                    : app.Environment.WebRootPath;
                uploadsPath = Path.Combine(webRoot, "uploads");
            }
            else if (Path.IsPathRooted(configuredUploadsPath))
            {
                uploadsPath = configuredUploadsPath;
            }
            else
            {
                uploadsPath = Path.Combine(app.Environment.ContentRootPath, configuredUploadsPath);
            }

            try
            {
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
                    RequestPath = "/uploads",
                    ServeUnknownFileTypes = false
                });
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "No se pudo configurar el proveedor de archivos en '{Path}'", uploadsPath);
            }

            app.UseRouting();
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            // Middleware de logging para rutas /api/
            app.UseMiddleware<ApiLoggingMiddleware>();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages().WithStaticAssets();

            // SignalR hubs
            app.MapHub<ADMISION.Hubs.NotificationHub>("/hubs/notifications");

            // Hangfire Dashboard restringido a SuperAdmin
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
            });

            using (var scope = app.Services.CreateScope())
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                var timeZoneId = builder.Configuration["Jobs:TimeZoneId"] ?? "SA Pacific Standard Time";

                TimeZoneInfo tz;
                try { tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
                catch { tz = TimeZoneInfo.CreateCustomTimeZone("PET", TimeSpan.FromHours(-5), "Peru Time", "Peru Time"); }

                recurringJobManager.AddOrUpdate<ModalityStatusJob>(
                    "deactivate-expired-modalities",
                    job => job.CheckAndDeactivateModalitiesAsync(),
                    "5 0 * * *",
                    new RecurringJobOptions { TimeZone = tz });
            }

            app.Run();
        }
    }
}
