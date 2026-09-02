# ADMISION — Proyecto Web

Portal web del **Sistema de Admisión** construido sobre **ASP.NET Core MVC + Razor Pages (.NET 10)**. Alberga el sitio público de inscripción y el panel administrativo completo.

---

## Arquitectura

```
ADMISION/
├── Controllers/       ← Controladores MVC (Admin, Public, Api)
├── Pages/             ← Vistas Razor (públicas, admin, shared)
├── Services/          ← Lógica de aplicación (55+ servicios)
├── Hubs/              ← SignalR (NotificationHub)
├── Middleware/         ← ApiLoggingMiddleware
├── Extensions/        ← Métodos de extensión
├── Models/ViewModels/ ← ViewModels por feature
├── Data/              ← DbInitializer (seed)
├── Infrastructure/    ← Hangfire authorization
├── Templates/         ← Plantillas HTML (Scriban + Puppeteer)
├── wwwroot/           ← CSS, JS, fuentes, iconos, uploads
├── Program.cs         ← Pipeline ASP.NET Core
└── appsettings.json   ← Configuración (no versionado)
```

### Patrón

- **MVC + Razor Pages híbrido**: controladores con atributos de ruta; vistas en `Pages/` que funcionan como vistas de MVC.
- **Servicios con interfaz**: toda la lógica reutilizable vive en `Services/Interfaces/` + `Services/Implementations/`, registrados como `Scoped` en DI.
- **Inyección de `AppDbContext` directa** en controladores y servicios (sin repositorios).
- **Auditoría transversal**: `AuditInterceptor` (EF Core `SaveChangesInterceptor`) audita INSERT/UPDATE/DELETE automáticamente.

---

## Controllers por área

### Admin (Backoffice)

| Módulo | Controllers |
|---|---|
| Dashboard | `AdminController` |
| Usuarios | `UsersController` |
| Perfil | `ProfileController` |
| Acceso | `AccessController` |
| Configuración | `ConfigController`, `ExternalApisController`, `PersonQueriesController` |
| Periodos | `TermsController` |
| Facultades | `FacultiesController` |
| Carreras | `CareersController` |
| Modalidades | `ModalitiesController`, `TypeModalitiesController`, `VacanciesController` |
| Tipos postulante | `TypePostulantInscriptionController`, `TypePostulantRequisitesController` |
| Requisitos | `RequirementsController`, `ModalityRequisitesController` |
| Áreas temáticas | `TematicAreasController` |
| Procesamiento exámenes | `ExamProcessingController`, `ExamResultsController` |
| Discapacidades | `DisabilityTypeController` |
| Colegios | `SchoolManagementController` |
| Postulantes | `PostulantsController`, `ReportController` |
| Asistencia | `AttendanceController` |
| Info pública | `BannersController`, `ProspectsController`, `SyllabiController`, `RegulationsController`, `OtherFilesController`, `PublicInfosController`, `BrochuresController`, `FaqController`, `BeneficiariesController` |
| Cronograma | `ScheduleController` |
| Infraestructura | `ClassroomsController`, `PavilionsController`, `ExamAssignmentController` |
| Económico | `PaymentCodesController`, `MethodsPaymentController`, `EconomicManagementController` |
| Documental | `DocumentTypesController`, `DocumentHeadersController`, `DocumentIssuanceController`, `AcademicYearsController` |
| Notificaciones | `NotificationsController` |
| Reportes | `TematicAreaReportController` |
| Ubigeo | `UbigeoController` |

### Public (Portal)

| Controller | Rutas principales |
|---|---|
| `HomeController` | `/public` — landing, inscripción, resultados, consultas |
| `LoginController` | `/login` — autenticación |
| `ConfigController` | `/public/config` — configuración pública |
| `ChatbotController` | `/chatbot` — FAQ widget |
| `InscriptionDocumentController` | subida de documentos del postulante |

### Api (REST)

| Controller | Ruta | Auth |
|---|---|---|
| `AuthController` | `api/auth` | None (emite JWT) |
| `PostulantsController` | `api/v1/postulants` | JWT Bearer (rol `ApiConsumer`) |

---

## Servicios

### Infraestructura
- `IAuthService`, `ICaptchaService`, `IConfigService`, `IFileService`, `IPasswordHasher`, `IFingerprintService`
- `IApiLogService`, `IExternalApiService`
- `INotificationService`, `IDocumentService`, `IConstanciaIngresoPdfRenderer`

### Consulta/CRUD de dominio
- `IUbigeoService`, `ICatalogService`, `IFacultyService`, `ICareerService`, `ISchoolService`
- `IModalityService`, `IVacancyService`, `ITypeModalityService`, `ITermService`
- `IPaymentCodeService`, `IBannerService`, `IProspectService`, `IOtherFilesService`
- `IPublicInfoService`, `IScheduleEventService`, `IBeneficiaryService`, `IBrochureService`
- `IDisabilityTypeService`, `IFaqService`, `IInscriptionDocumentService`
- `ITypePostulantInscriptionService`, `IFileRequirementService`, `ITypePostulantRequisiteService`
- `IModalityRequisiteService`, `IExamResultService`
- `IClassroomService`, `IExamAssignmentService`, `IExamProcessingService`
- `IDocumentTypeService`, `IDocumentHeaderService`, `IDocumentIssuanceService`
- `IProfileService`, `IUserManagementService`, `IPostulantQueryService`
- `ITematicAreaService`, `IAttendanceService`, `IDashboardService`
- `ITematicAreaReportService`, `IPostulantResumeService`

### Portal público
- `IPublicPortalService`, `IInscriptionLookupService`, `IInscriptionService`

---

## Frontend

- **Tailwind 3.4** con paleta personalizada (primary `#f54477`, secondary `#716aca`)
- **Tabler Icons** self-hosted (clase `ti ti-*`)
- **Inter Variable** font self-hosted
- **SweetAlert2**, **Toastify**, **Flatpickr**, **Chart.js**, **SignalR**, **Leaflet** (mapas)
- **Partials Razor**: `_DataTable`, `_Modal`, `_FormInput`, `_FormDate`, `_FormCheckbox`, `_Combobox`, `_PageHero`, `_FilterBar`, `_SearchInput`, `_CustomSelect`, `_CustomDropzone`

Ver la [documentación de design system](../README.md#9-design-system) en el README principal.

---

## Seguridad

- Autenticación por cookie endurecida (`__Host-Admision.Auth`, `HttpOnly`, `SameSite=Lax`)
- JWT Bearer para API consumers (rol `ApiConsumer`)
- Antiforgery global (`AutoValidateAntiforgeryTokenAttribute`)
- Rate limiting: login (5/min), public-post (10/min), public-lookup (30/min), api (120/min)
- CAPTCHA: Cloudflare Turnstile / Google reCAPTCHA (configurable)
- Hangfire Dashboard restringido a `SuperAdmin`/`Soporte`
- Data Protection keys persistidas a disco

---

## Background jobs

- **Hangfire** con almacenamiento PostgreSQL
- **`ModalityStatusJob`**: desactiva modalidades vencidas (cron diario 00:05 PET)

---

## Señalización

- **SignalR** `NotificationHub` en `/hubs/notifications` — notificaciones en tiempo real al panel admin
