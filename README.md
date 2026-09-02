# Sistema de Admisión (ADMISION)

Portal de admisión universitaria construido sobre **ASP.NET Core (.NET 10)** + **Entity Framework Core 10** sobre **PostgreSQL**. Cubre el portal público (información, inscripción, resultados) y el portal administrativo (gestión académica, económica, documental, biométrica y reportes).

Este documento describe la **estructura**, los **frameworks y librerías** usados, la **arquitectura del sistema**, la **proyección de escalabilidad** y las **indicaciones de mantenimiento y actualización**.

---

## 1. Estructura del repositorio

```
sistema-de-inscripciones/
├── ADMISION.slnx                 ← Solución principal (web + entidades)
├── ADMISION/                     ← Proyecto Web (MVC + Razor Pages + SignalR)
│   ├── Controllers/
│   │   ├── Admin/                ← Backoffice (~35 controladores agrupados por módulo)
│   │   │    ├── AccessController, AdminController, ConfigController
│   │   │    ├── DocumentaryManagementController/
│   │   │    ├── EconomicManagementController/
│   │   │    ├── ExamManagementController/  (Careers, Faculties, Modalities, ExamProcessing…)
│   │   │    ├── InfoManagementController/  (Banners, Prospects, Syllabi, Regulations…)
│   │   │    ├── InfoPostulant/             (Postulants, Attendance, Report)
│   │   │    ├── InfrastructureController/  (Classrooms, Pavilions, ExamAssignment)
│   │   │    ├── ReportsController/         (TematicAreaReport)
│   │   │    ├── TermController/            (Terms)
│   │   │    └── UserController/            (Users)
│   │   ├── Public/               ← Sitio público (Home, Login, Config, Chatbot)
│   │   └── Api/                  ← Endpoints REST (Auth, Postulants V1)
│   ├── Pages/
│   │   ├── Admin/                ← ~50+ vistas Razor del backoffice por módulo
│   │   ├── Public/               ← Vistas Razor del sitio público (~15)
│   │   ├── Shared/               ← Layouts (_PublicLayout, _AdminLayout) + partials
│   │   └── Shared/Components/    ← Partials de componentes (Banner, Footer, Header…)
│   ├── Services/
│   │   ├── Interfaces/           ← 55+ contratos de servicios de aplicación
│   │   ├── Implementations/      ← Auth, Banner, Captcha, Career, Catalog, Config,
│   │   │                            Dashboard, Document*, Exam*, ExternalApi, Faculty,
│   │   │                            File, Fingerprint, Inscription*, Notification,
│   │   │                            PasswordHasher, PaymentCode, Postulant*, Profile,
│   │   │                            Prospect, Public*, Schedule, School, Term, Ubigeo…
│   │   ├── Background/           ← Jobs Hangfire (ModalityStatusJob)
│   │   └── Interceptors/         ← AuditInterceptor (EF Core SaveChangesInterceptor)
│   ├── Hubs/                     ← NotificationHub (SignalR, ruta /hubs/notifications)
│   ├── Infrastructure/Hangfire/  ← HangfireDashboardAuthorizationFilter
│   ├── Middleware/               ← ApiLoggingMiddleware
│   ├── Extensions/               ← Métodos de extensión
│   ├── Models/ViewModels/        ← VMs por feature (Admin/Public/Reports)
│   ├── Templates/Documents/      ← Plantillas HTML para Scriban + PuppeteerSharp
│   ├── Data/                     ← DbInitializer (seed)
│   ├── DataProtection-Keys/      ← Claves de Data Protection persistidas (no versionar)
│   ├── wwwroot/                  ← Assets estáticos, librerías cliente, geojson, uploads
│   ├── Pages/_ViewImports.cshtml
│   ├── Program.cs                ← Composición del pipeline ASP.NET Core
│   ├── appsettings.json          ← Ignorado por git (secretos locales)
│   ├── Original.appsettings.json ← Plantilla versionada con placeholders
│   ├── package.json / tailwind.config.js
│   └── libman.json               ← Bootstrap, jQuery, jQuery Validate (CDNJS)
│
├── ADMISION.ENTITIES/            ← Class library: dominio + EF Core
│   ├── Constants/
│   │   ├── AppConstants.cs       ← Roles, estados de inscripción, tipos de archivo
│   │   └── ConfigGeneral.cs      ← Claves de configuración del sistema (UNAMAD)
│   ├── Data/AppDbContext.cs      ← ~55 DbSet<>, OnModelCreating con FK explícitas
│   ├── Migrations/               ← Migraciones EF Core (InitialMigrate → AddBrochureTable → AddApiJwtTables)
│   └── Models/                   ← ~74 entidades agrupadas por dominio:
│       ├── Api/                  ApiRequestLog, ApiToken
│       ├── Biometrics/           Fingerprint, PostulantPhoto, PostulantAttendance
│       ├── DocumentaryManagement/ AcademicYearName, DocumentHeaderConfig,
│       │                          DocumentIssued, DocumentType
│       ├── EconomicManagement/   PaymentCode, PaymentCodeModality, Payments, MethodPayment
│       ├── Exam/                 ExamSession, ExamParameters, ExamAreaConfig,
│       │                          ExamAnswerKey, PostulantAnswerSheet,
│       │                          PostulantAnswer, ExamScoreResult
│       ├── Info/                 Banner, Brochure, FaqItem, OtherFiles, Prospect,
│       │                          PublicInfo, University
│       ├── Infrastructure/       Classroom, Pavilion, ExamAssignment
│       ├── Integrations/         ExternalApi, ApiQueryLog, ExternalAcademicInfo,
│       │                          ExternalPaymentDetail, ExternalPaymentVoucher
│       ├── Modality/             Career, CareerImage, Faculty, Modality,
│       │                          ModalityCareer, Term, TypeModality,
│       │                          TypeModalityCareer, Vacancies, Beneficiarie,
│       │                          TematicArea, TematicAreaCareer, ScheduleEvent,
│       │                          ExamResult
│       ├── Notifications/        Notification, NotificationView
│       ├── Postulant/            Postulant, Inscription, Parent, Resignation,
│       │                          Observations, FileSubmission, DisabilityType,
│       │                          PostulantDisability, TypePostulantInscription
│       ├── Requirement/          FileRequirementManagement, ModalityRequisite,
│       │                          TypePostulantRequisite
│       ├── Schools/              Schools
│       ├── System/               Audit, AccessLog, Config
│       ├── Ubigeo/               Country, Department, Provincie, Distrit
│       └── Users/                Users, Rols, UserRol, Teachers, Observations
└── README.md                     ← (este archivo)
```


---

## 2. Frameworks y librerías

### Backend (.NET 10)

| Tipo | Paquete | Versión | Rol |
|---|---|---|---|
| Runtime | `Microsoft.NET.Sdk.Web` | net10.0 | Razor Pages + MVC |
| ORM | `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | Driver EF Core para PostgreSQL |
| ORM | `Microsoft.EntityFrameworkCore.Design` / `.Tools` | 10.0.2 | Migraciones (`dotnet ef`) |
| Jobs | `Hangfire`, `Hangfire.AspNetCore` | 1.8.23 | Servidor de jobs + dashboard |
| Jobs | `Hangfire.PostgreSql` | 1.21.1 | Storage de Hangfire en PostgreSQL |
| Excel | `ClosedXML` | 0.105.0 | Exportación de reportes a XLSX |
| PDF | `QuestPDF` | 2024.12.3 | Generación de PDFs (vouchers, fichas) |
| PDF | `PuppeteerSharp` | 20.0.5 | Render HTML→PDF para plantillas complejas |
| Plantillas | `Scriban` | 7.0.0 | Plantillas HTML/texto para documentos |
| Realtime | `Microsoft.AspNetCore.SignalR` | (built-in) | `NotificationHub` para panel admin |
| Hashing | `Microsoft.AspNetCore.Identity` | (built-in) | `PasswordHasher<T>` (PBKDF2) |
| Auth | `CookieAuthentication` | (built-in) | Cookies endurecidas (`__Host-`, SameSite=Lax) |
| Antiforgery | (built-in) | — | `AutoValidateAntiforgeryTokenAttribute` global |
| Rate limit | `Microsoft.AspNetCore.RateLimiting` | (built-in) | Políticas `login`, `public-post`, `public-lookup` |
| CAPTCHA | Cloudflare Turnstile / Google reCAPTCHA | — | `ICaptchaService` — login, inscripción y `check-user` |
| Data Protection | (built-in) | — | Llaves persistidas en `DataProtection-Keys/` |

### Frontend

| Tipo | Paquete | Versión | Rol |
|---|---|---|---|
| CSS | `tailwindcss` | 3.4.17 | Tailwind CSS para vistas Razor |
| CLI | `@tailwindcss/cli` | 4.1.18 | Compilador Tailwind |
| Iconos | **Tabler Icons** (`@tabler/icons-webfont`) self-hosted en `wwwroot/lib/tabler-icons/` | 3.44.0 | Iconografía outline (clase `ti ti-*`) — reemplaza al FontAwesome legacy |
| Fuente | **Inter Variable** self-hosted en `wwwroot/lib/inter/` (woff2 + Italic, OFL 1.1) | 4.0 | Tipografía UI sin dependencia de Google Fonts (preload en layout) |
| Alertas | `sweetalert2` | ^11.26 | Modales |
| Toasts | `toastify-js` | ^1.12 | Notificaciones |
| Compat | Bootstrap 5.3, jQuery 3.7, jQuery-Validate (vía LibMan/CDNJS) | — | Validación cliente legacy |
| Mapas | Leaflet + GeoJSON local (`wwwroot/data/geo/`) | — | Choropleth en dashboard |


---

## 3. Arquitectura del sistema

### 3.1. Vista lógica

```
┌───────────────────────────────────────────────────────────────────────┐
│                     ADMISION (Web — net10)                            │
│                                                                       │
│  ┌──────────────┐   ┌─────────────┐   ┌─────────────────────────┐    │
│  │ Controllers  │   │ Razor Pages │   │ Services (Scoped)       │    │
│  │  Admin/      │   │  Admin/     │   │  Auth, Banner, Config,  │    │
│  │  Public/     │   │  Public/    │   │  Document, ExamAssign-  │    │
│  │  Api/        │   │  Biometrics/│   │  ment, ExamProcessing,  │    │
│  └──────┬───────┘   └──────┬──────┘   │  Exam, File, Finger-    │    │
│         │                  │          │  print, Notification,   │    │
│         └──────┬───────────┘          │  PasswordHasher,        │    │
│                │                      │  PostulantCode          │    │
│                ▼                      └────────────┬────────────┘    │
│        ┌───────────────┐  ◄─ AuditInterceptor ─────┘                  │
│        │ AppDbContext  │                                              │
│        └───────┬───────┘                                              │
│                │            ┌──────────────────┐                      │
│                │            │ SignalR          │                      │
│                │            │ NotificationHub  │ ◄── /hubs/notifications
│                │            └──────────────────┘                      │
│                │            ┌──────────────────┐                      │
│                │            │ Hangfire Server  │                      │
│                │            │ + Dashboard      │ ◄── /hangfire (auth)│
│                │            └────────┬─────────┘                      │
│                │                     │  ModalityStatusJob (cron)      │
└────────────────┼─────────────────────┼───────────────────────────────┘
                 │                     │
                 ▼                     ▼
        ┌──────────────────┐    ┌────────────┐      ┌───────────────────┐
        │ ADMISION.        │    │ Hangfire   │      │ BiometricBridge   │
        │ ENTITIES         │◄──►│ Storage    │      │ (localhost:5000)  │
        │ - Models / DTOs  │    └────────────┘      │ ZK9500 SDK        │
        │ - AppDbContext   │                        └─────────┬─────────┘
        │ - Migrations     │                                  │
        └────────┬─────────┘                                  │  HTTP
                 │                                            │
                 ▼                                            ▼
              PostgreSQL ◄───────── reportes/exports ──── Cliente Web
```

### 3.2. Patrón arquitectónico

El proyecto es un **monolito en capas (layered monolith)** con separación pragmática por feature:

- **Capa de presentación**: MVC + Razor Pages **híbrido**. Los `Controllers/` exponen endpoints atributados (`[Route]`, `[HttpGet/Post]`, `[Authorize(Roles=...)]`); las vistas viven en `Pages/` con `_PublicLayout.cshtml` y `_AdminLayout.cshtml`.
- **Capa de aplicación**: servicios `Scoped` con interfaz + implementación, registrados en `Program.cs`. Encapsulan lógica reutilizable (autenticación, hashing, archivos, biometría, generación de documentos, asignación y procesamiento de exámenes, código de postulante, notificaciones).
- **Capa de dominio + datos**: `ADMISION.ENTITIES` contiene los modelos agrupados por subdominio y un único `AppDbContext` (~55 `DbSet<>`) con configuración Fluent en `OnModelCreating`. No hay repositorios: los controladores **inyectan `AppDbContext` directamente**.
- **Transversal**: `AuditInterceptor` (audita INSERT/UPDATE/DELETE con redacción de campos sensibles), Data Protection persistido a disco, antiforgery global, rate limiting y autenticación por cookie endurecida.
- **Tiempo real**: SignalR (`NotificationHub`) empuja notificaciones al panel admin sin polling.
- **Procesos asíncronos**: Hangfire con storage en PostgreSQL; el dashboard `/hangfire` está protegido por `HangfireDashboardAuthorizationFilter`.
- **Integraciones externas**: `BiometricBridge` (HTTP local hacia el SDK ZKTeco), Puppeteer (Chromium descargado en runtime para HTML→PDF), QuestPDF para vouchers y fichas.

### 3.3. ¿Es buena o mala arquitectura?

**Honesto:** es una arquitectura **adecuada para el alcance actual**, con decisiones pragmáticas correctas y deuda técnica visible. No es "mala" — es típica de un monolito en crecimiento.

**Lo que está bien**

- ✅ Separación física clara entre `ADMISION` (web) y `ADMISION.ENTITIES` (dominio + EF). Permite reutilizar el modelo si más adelante surge un servicio o consola.
- ✅ **Feature folders** (modelos por subdominio, controladores por área `Admin/Public`). Es fácil saber dónde tocar.
- ✅ Servicios con interfaz desacoplada → testeable en teoría (cuando existan pruebas).
- ✅ DI estricto, todos `Scoped`, sin singletons mutables.
- ✅ Endurecimiento de seguridad ya aplicado: antiforgery global, cookies `__Host-`, rate limiting, dashboard de Hangfire protegido, redacción en auditoría, `Original.appsettings.json` como plantilla.
- ✅ Migrations EF Core versionadas, `OnModelCreating` con `DeleteBehavior.Restrict` por defecto (evita cascadas accidentales).
- ✅ Auditoría granular vía interceptor — rara en proyectos a este nivel.


---

## 4. Proyección de escalabilidad

| Eje | Estado actual | Techo razonable | Bloqueadores principales |
|---|---|---|---|
| **Usuarios concurrentes (web)** | OK con 1 servidor + PostgreSQL bien dimensionado | ~1.000–3.000 simultáneos en pico de inscripciones | Listados sin paginación, falta de caché, `Include` profundos, dashboards con `ToList` sin proyección |
| **Volumen de datos** | OK para 1 institución / 1–2 procesos al año | Millones de filas en `Audits`, `AccessLogs`, `PostulantAnswers` antes de degradarse | Tabla `Audits` crece sin política de retención; falta partición temporal; índices ausentes en `Users.Document`, `Inscriptions.PostulantId+ModalityId`, `Audits.Timestamp` |
| **Escalado horizontal (web)** | Limitado | 1 instancia hoy; 2–3 con cambios menores | DataProtection-Keys en disco local (necesita volumen compartido o Redis), SignalR sin backplane, sesiones en cookie OK pero `Hangfire` corre embebido en el proceso |
| **Background jobs** | 1 worker dentro del proceso web | Suficiente para el job actual (1 cron diario) | Para cargas mayores conviene servidor Hangfire dedicado o cola externa |
| **Almacenamiento de archivos** | Disco local (`wwwroot/uploads` o `BaseStoragePath`) | Limitado por disco del servidor | Para escalar horizontal, mover a S3/Azure Blob; `FileService` ya está abstraído y permite el cambio sin tocar callers |
| **Generación de PDF** | Puppeteer descarga Chromium en runtime | Cada instancia tiene su Chromium; CPU-bound | Mover a job/queue si el volumen crece (cientos de PDFs/min) |


### Acciones de bajo costo y alto impacto

1. `AsNoTracking()` en consultas de lectura (dashboard, listados, JSON) → reduce uso de memoria y mejora throughput.
2. **Paginación** en listados administrativos (`Users`, `Inscriptions`, `Payments`).
3. **Proyecciones `Select`** en lugar de `Include` profundos en JSON.
4. **Caché en memoria** (`IMemoryCache`) para tablas casi inmutables (`Terms`, `Countries/Departments/Provinces/Districts`, `DisabilityTypes`, `Configs`).
5. **Índices SQL** sobre columnas de búsqueda (`Users.Document`, `Audits.Timestamp`, `Inscriptions.PostulantId+ModalityId`).
6. **Partición/archivado de `Audits`**: el interceptor se ejecuta en cada `SaveChanges` y la tabla crece linealmente con el uso.
7. **Compresión de respuesta** (`AddResponseCompression`) y bundling/minificación de CSS/JS.

---

## 5. Mantenimiento y actualizaciones

### 5.1. Compilar y ejecutar localmente

Pre-requisitos:
- .NET 10 SDK
- PostgreSQL 15+
- Node 20+ (solo para recompilar Tailwind)
- Opcional: Inno Setup (si vas a regenerar el instalador del puente biométrico)

```powershell
# 1) Restaurar y compilar la solución principal
dotnet restore ADMISION.slnx
dotnet build ADMISION.slnx

# 2) Configurar secretos locales (NO commitear appsettings.json)
cd ADMISION
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ADMISION.DB.UNAMAD;Username=postgres;Password=<real>"
dotnet user-secrets set "Admin:InitialPassword" "<contraseña-fuerte>"

# 3) Restaurar herramientas locales (dotnet-ef, etc.)
dotnet tool restore

# 4) Aplicar migraciones
dotnet ef database update --project ../ADMISION.ENTITIES --startup-project .

# 5) Ejecutar
dotnet run
```

> Si `Admin:InitialPassword` queda vacío, en el primer arranque se genera una contraseña aleatoria de 20 caracteres y se emite un log `Warning` con esa contraseña. Anótala, cámbiala desde el panel de usuarios y reinicia.

### 5.2. Tailwind CSS

```powershell
cd ADMISION
npm install
npm run build:css      # arranca el watcher de Tailwind 3
```

Cualquier cambio en `wwwroot/css/input.css` o en clases dentro de `Pages/**/*.cshtml` debe regenerar `wwwroot/css/output.css`.

### 5.3. Migraciones EF Core

#### Instalar la herramienta `dotnet-ef`

```powershell
dotnet tool install --local dotnet-ef --version 10.0.*
```

> La herramienta se instala como herramienta local del proyecto (repositorio) y queda registrada en `dotnet-tools.json`.

#### Crear una migración nueva

Después de modificar entidades en `ADMISION.ENTITIES/Models/` o cambiar configuración en `AppDbContext`:

```powershell
dotnet ef migrations add <NombreEnPascalCase> `
  --project ADMISION.ENTITIES `
  --startup-project ADMISION
```

**Ejemplo real:**

```powershell
dotnet ef migrations add ChangeModalityDatesToDateTime `
  --project ADMISION.ENTITIES `
  --startup-project ADMISION
```

Esto genera 3 archivos en `ADMISION.ENTITIES/Migrations/`:
- `<Timestamp>_<Nombre>.cs` — código `Up()` y `Down()` de la migración
- `<Timestamp>_<Nombre>.Designer.cs` — snapshot del modelo al momento de la migración
- `AppDbContextModelSnapshot.cs` — snapshot actualizado

#### Aplicar migraciones a la BD

```powershell
# Aplicar todas las migraciones pendientes
dotnet ef database update `
  --project ADMISION.ENTITIES `
  --startup-project ADMISION

# Aplicar hasta una migración específica (revertir a una versión)
dotnet ef database update 20260715153441 `
  --project ADMISION.ENTITIES `
  --startup-project ADMISION
```

#### Generar SQL para despliegue manual

Útil para revisar el SQL antes de ejecutar o para desplegar en servidores sin SDK:

```powershell
# Script completo desde cero
dotnet ef migrations script `
  --project ADMISION.ENTITIES `
  --startup-project ADMISION `
  -o migracion.sql

# Script solo de una migración específica
dotnet ef migrations script 20260718010327 `
  --project ADMISION.ENTITIES `
  --startup-project ADMISION `
  -o migracion_parcial.sql
```

#### Listar migraciones aplicadas vs pendientes

```powershell
dotnet ef migrations list `
  --project ADMISION.ENTITIES `
  --startup-project ADMISION
```

#### Errores comunes y soluciones

| Error | Causa | Solución |
|---|---|---|
| `Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone'` | El tipo de columna en PostgreSQL es `timestamp with time zone`, pero el `DateTime` no tiene `Kind=Utc` | Usar `timestamp without time zone` en la migración, o marcar el valor como UTC con `DateTime.SpecifyKind(date, DateTimeKind.Utc)` |
| `The migration 'XXXXXX' was not found` | Se intenta revertir a una migración que no existe en el ensamblado | Verificar el nombre con `dotnet ef migrations list` |
| `Foreign key '...' is not nullable` | `Down()` intenta converter una columna NOT NULL a NULL sin valores por defecto | Agregar `defaultValue` temporal en el `Down()` o ejecutar el `Down()` manualmente vía SQL |
| `A pending model changes were detected` | El snapshot no coincide con el modelo actual | Ejecutar `dotnet ef migrations add SnapshotFix --project ADMISION.ENTITIES --startup-project ADMISION` para sincronizar |
| `The property 'X' is not navigable` | Falta `.Include()` o `.Navigation()` en el modelo | Revisar relaciones en `AppDbContext.OnModelCreating` |

#### Notas importantes

- Verifica que `OnModelCreating` no rompa relaciones existentes (los borrados son `Restrict` por defecto — un FK roto bloquea el `update`).
- **Nunca editar una migración ya aplicada** a menos que también actualices `__EFMigrationsHistory` y la base de datos directamente.
- Para cambios de tipo de columna con datos existentes, considera si `Up()` preserva los datos o los transforma.
- PostgreSQL: `timestamp with time zone` almacena en UTC y convierte al leer; `timestamp without time zone` almacena tal cual. Para fechas locales sin zona horaria, usar `without time zone`.

### 5.4. Actualizar paquetes NuGet

```powershell
# Listar actualizaciones disponibles
dotnet list ADMISION.slnx package --outdated

# Actualizar uno específico (preferible)
dotnet add ADMISION/ADMISION.csproj package <Paquete> --version <X.Y.Z>

# Tras actualizar EF Core o Npgsql, regenerar y revisar el snapshot
dotnet build
```

**Cuidados especiales:**
- **EF Core / Npgsql**: subir `Microsoft.EntityFrameworkCore.*` y `Npgsql.EntityFrameworkCore.PostgreSQL` **juntos** y a versiones compatibles. Tras actualizar, regenerar el snapshot revisando que no haya `model-changes-pending`.
- **Hangfire**: respetar la compatibilidad con `Hangfire.PostgreSql`. Cambios mayores requieren `SetDataCompatibilityLevel`.
- **PuppeteerSharp**: descarga su propio Chromium la primera vez que se llama; revisa el directorio cache (`Local/Puppeteer`) en el server.
- **QuestPDF**: licencia comunitaria activada en `Program.cs`. Si la organización pasa a uso comercial fuera de los términos de Community, comprar licencia.

### 5.5. Despliegue

- Sitio web: publicar `dotnet publish -c Release` para Windows (IIS) o Linux (Kestrel + reverse proxy).
- **Importante en IIS**: las llaves de Data Protection se persisten en `DataProtection-Keys/` dentro de `ContentRoot`. Asegurar permisos de escritura del App Pool sobre esa carpeta. Sin esto, los reciclos invalidan sesiones y antiforgery.
- `appsettings.json` **no se versiona**. En cada entorno usar `appsettings.Production.json` o variables de entorno (`ConnectionStrings__DefaultConnection`, `Admin__InitialPassword`).
- Carpeta de archivos: configurar `FileUpload:BaseStoragePath` apuntando a un volumen externo (ej. `C:\inetpub\Files\admision`). Si se escala a varias instancias, usar almacenamiento compartido (UNC, SMB, o cambiar `FileService` a S3/Azure Blob).
- Tipos permitidos en `FileUpload:AllowedExtensions` / `AllowedMimeTypes`: PDF + JPG/JPEG/PNG/GIF/WebP/BMP/ICO (los 7 con magic-byte validation en `FileService`). SVG y AVIF están **excluidos a propósito** (SVG = riesgo XSS por scripts embebidos; AVIF no tiene firma binaria registrada).
- Hangfire dashboard: `/hangfire` solo es accesible a roles `SuperAdmin` / `Soporte` autenticados.
- Cron: el job `deactivate-expired-modalities` corre a las 00:05 hora Perú. Configurable vía `Jobs:TimeZoneId`.


Distribuir `BiometricBridge_Setup.exe` a las máquinas con lector ZK9500. Detalles en `BiometricBridge/README.md`.

### 5.6. Higiene del repositorio

Mantener fuera del repo (ya cubiertos en `.gitignore`):
- `bin/`, `obj/`, `node_modules/`
- `appsettings.json` (versionar solo `Original.appsettings.json`)
- `wwwroot/uploads/` y archivos de procesos (`wwwroot/2026`)
- `Properties/PublishProfiles/`
- `DataProtection-Keys/`

Limpiar cuando aparezcan: `Index copy.cshtml`, `TestComponents.cshtml`, `fix_comments.ps1`, `*.csproj.Backup.tmp`.

### 5.7. Observabilidad y operación

- Logs: `ILogger` por defecto a consola. Para producción, considerar Serilog + sink a archivo o Seq.
- Health checks recomendados: `AddHealthChecks().AddDbContextCheck<AppDbContext>().AddHangfire()`. Endpoint sugerido `/health`.
- Auditoría: revisar `Audits` periódicamente; planear retención (ej. 12 meses) y archivado.
- Hangfire: monitorear el dashboard ante jobs fallidos.
- Backups: `pg_dump` programado + restore drill mensual.

---

## 6. Estado de seguridad (resumen)

El endurecimiento principal está aplicado: `UseAuthentication()` antes de `UseAuthorization()`, cookies `__Host-` con `HttpOnly + SameSite=Lax + SecurePolicy=Always` (en prod), antiforgery global, rate limiting (`login` 5/min, `public-post` 10/min, `public-lookup` 30/min), dashboard de Hangfire protegido, contraseña inicial del `admin` configurable o aleatoria, redacción de campos sensibles en el `AuditInterceptor`, `AllowedHosts` restringido, secretos fuera del repo.

### CAPTCHA (Cloudflare Turnstile / reCAPTCHA)

Servicio `ICaptchaService` configurable vía `appsettings.json`:

```json
"Captcha": {
  "Provider": "Turnstile",        // "Turnstile" (default) | "ReCaptcha"
  "Enabled": false,                // En false el servicio acepta cualquier token (modo dev)
  "SiteKey": "<site-key>",
  "SecretKey": "<secret-key>"
}
```

Endpoints protegidos cuando `Captcha:Enabled = true`:

| Endpoint | Modo | Token |
|---|---|---|
| `POST /login` | Widget visible al final del formulario | Field `cf-turnstile-response` / `g-recaptcha-response` |
| `POST /inscription/register` | Widget visible antes de "Finalizar inscripción" | Field `cf-turnstile-response` / `g-recaptcha-response` |
| `GET /public/check-user` | Widget **invisible** que se ejecuta en background y se refresca tras cada uso (single-use token) | Header `X-Captcha-Token` |

**Claves de prueba (Cloudflare Turnstile, always-pass)** ya configuradas por defecto:
- SiteKey: `1x00000000000000000000AA`
- SecretKey: `1x0000000000000000000000000000000AA`

En producción reemplazar por claves reales emitidas en `https://dash.cloudflare.com/?to=/:account/turnstile` (gratis, sin límite) o `https://www.google.com/recaptcha/admin`. Las pruebas pueden hacerse con `Captcha:Enabled=false` para no requerir conectividad externa.

### Integraciones de APIs externas (`/admin/config/apis`)

Servicio `IExternalApiService` que centraliza la invocación de cualquier API HTTP registrada por un administrador. Cada consulta queda auditada con usuario, IP, parámetros, status, duración y un excerpt de la respuesta (8 KB).

**Modelo (schema `Integrations`):**
- `ExternalApi`: nombre, descripción, método (`GET/POST/PUT/DELETE`), URL con placeholders `{nombreParametro}`, autenticación (`None/Bearer/ApiKey/Basic`), headers JSON, plantilla de body, definición de campos a mostrar en el resultado.
- `ApiQueryLog`: historial inmutable. Las APIs con consultas registradas no se borran — se desactivan (soft-delete) para preservar la auditoría.

**UI Admin:**

| Ruta | Descripción |
|---|---|
| `/admin/config/apis` | Listado de APIs registradas |
| `/admin/config/apis/nuevo` | Registrar una API nueva |
| `/admin/config/apis/editar/{id}` | Editar (deja vacío "Valor / Token" para conservar el secreto) |
| `/admin/config/apis/consultar/{id}` | Probar consulta — el formulario se genera dinámicamente según los parámetros declarados, y el resultado JSON se renderiza como tabla legible |
| `/admin/config/apis/logs` | Historial filtrable por API |

**Ejemplo (consulta DNI tipo RENIEC):**

```text
URL:        https://api.servicio.com/dni/{document}
HttpMethod: GET
AuthType:   ApiKey
AuthHeader: Authorization
AuthValue:  Bearer xxxxxxxxxx

RequestParametersJson:
[{"key":"document","label":"Número de DNI","required":true}]

ResponseFieldsJson:
[
  {"path":"data.nombres",          "label":"Nombres"},
  {"path":"data.apellidoPaterno",  "label":"Apellido Paterno"},
  {"path":"data.apellidoMaterno",  "label":"Apellido Materno"}
]
```

Si `ResponseFieldsJson` se deja vacío, la respuesta JSON se aplana automáticamente y se muestra como pares clave/valor. Si la respuesta no es JSON, se muestra como texto crudo.

El `AuditInterceptor` redacta automáticamente `AuthValue` (junto con `Password`, `Token`, `Secret`, `ApiKey`, etc.) antes de serializar a la tabla `Audits`.


---

## 7. Patrón de servicios (centralización de consultas)

Migración en curso para sacar las consultas de los controladores hacia servicios reutilizables. El patrón:

1. **Interface** en `Services/Interfaces/I<Feature>Service.cs` — contrato con métodos asíncronos, `CancellationToken ct = default`, devolviendo entidades o DTOs/records simples (`CatalogOption`, `UbigeoOption`, etc.).
2. **Implementación** en `Services/Implementations/<Feature>Service.cs` — recibe `AppDbContext` y otros servicios via DI; encapsula filtros, ordenamientos, paginación y reglas básicas de negocio.
3. **Registro** en `Program.cs` como `AddScoped`.
4. **Controlador** solo delega: parsea HTTP → llama al servicio → devuelve View/Json/Redirect.

**Tipos compartidos** en `Models/Shared/PagedResult.cs`:
- `ListQuery`: parámetros base (`Search`, `SortBy`, `SortDir`, `Page`, `PageSize`). Heredar para añadir filtros tipados (ej. `CareerListQuery : ListQuery { public Guid? FacultyId }`).
- `PagedResult<T>`: resultado paginado con `CreateAsync(IQueryable<T>, page, pageSize, ct)`.
- `DeleteOutcome` enum: `Deleted | NotFound | HasDependencies` — los controladores muestran TempData según el caso.

**Servicios disponibles hoy:**

| Servicio | Rol | Consumido por |
|---|---|---|---|
| `IUbigeoService` | countries, departments, provinces, districts (cascading); import CSV | `UbigeoController`, `Public/HomeController` |
| `ICatalogService` | terms, faculties, careers, modalities, typeModalities, typePostulants — lookups read-only | Múltiples controllers admin |
| `IFacultyService` | CRUD completo de Faculty | `FacultiesController` |
| `ICareerService` | List paginado/ordenado + CRUD con archivos atómico (transacción EF + pre-validación + save-new-then-delete-old) | `CareersController` |
| `ISchoolService` | List paginado/filtrado por ubigeo + CRUD + import Excel | `SchoolManagementController` |
| `IModalityService` | List paginado + CRUD + validación de fechas dentro del periodo académico | `ModalitiesController` |
| `IVacancyService` | Matriz de vacantes (carrera × tipo de modalidad) | `VacanciesController` |
| `ITypeModalityService` | List paginado + CRUD de tipos de modalidad | `TypeModalitiesController` |
| `IPaymentCodeService` | CRUD con asociaciones `PaymentCodeModality` (replace-on-save) | `PaymentCodesController` |
| `IUserManagementService` | CRUD usuarios + sync roles + toggle bloqueo + perfil agregado | `UsersController` |
| `IPostulantQueryService` | List paginado de inscripciones (8 filtros) | `PostulantsController` |
| `ITematicAreaService` | CRUD + matriz carrera × área temática por término | `TematicAreasController` |
| `IAttendanceService` | Búsqueda + verificación biométrica (BiometricBridge HTTP) + asistencia manual | `AttendanceController` |
| `ITermService` | CRUD + replicación de configuración previa al crear término | `TermsController` |
| `IBannerService` | CRUD banner con imagen + lookup activos portal | `BannersController`, `Public/HomeController` |
| `IProspectService` | CRUD con archivo PDF + Term lookup | `ProspectsController` |
| `IOtherFilesService` | CRUD genérico sobre `OtherFiles` por categoría (sirve a 3 controllers) | `SyllabiController`, `RegulationsController`, `OtherFilesController` |
| `IPublicInfoService` | CRUD info pública (Term + Modality, sin archivo) | `PublicInfosController` |
| `IScheduleEventService` | CRUD eventos de cronograma por término | `ScheduleController` |
| `IBeneficiaryService` | CRUD con Term FK | `BeneficiariesController` |
| `IDisabilityTypeService` | CRUD + listado AJAX | `DisabilityTypeController` |
| `ITypePostulantInscriptionService` | List paginado + CRUD | `TypePostulantInscriptionController` |
| `IFileRequirementService` | CRUD con `RequirementDeleteOutcome` tipado | `RequirementsController` |
| `ITypePostulantRequisiteService` | Junction table con detección de duplicados | `TypePostulantRequisitesController` |
| `IModalityRequisiteService` | Junction triple con outcomes tipados | `ModalityRequisitesController` |
| `IExamResultService` | CRUD PDF + PublishedAt toggle | `ExamResultsController` |
| `IClassroomService` | CRUD salones + lookup pabellones activos | `ClassroomsController` |
| `IExamAssignmentService` | Sorteo + conteo/export por modalidad | `ExamAssignmentController` |
| `IDocumentTypeService` | CRUD + dedupe Code + listado templates HTML | `DocumentTypesController` |
| `IDocumentHeaderService` | CRUD con regla "single-active" | `DocumentHeadersController` |
| `IDocumentIssuanceService` | Búsqueda ingresantes + emisión PDF/ZIP | `DocumentIssuanceController` |
| `IProfileService` | Perfil usuario: lectura, update email único, cambio contraseña | `ProfileController` |
| `IDashboardService` | Agregación dashboard (KPIs, charts, mapas) | `AdminController` |
| `ITematicAreaReportService` | Reporte agregado por área temática | `TematicAreaReportController` |
| `IPostulantResumeService` | Resumen completo postulante + foto, huellas, edición nota | `ReportController` |
| `IExamProcessingService` | Procesamiento lector óptico + CRUD sesiones | `ExamProcessingController` |
| `IExternalApiService` | Invocación HTTP genérica + auditoría + CRUD | `ExternalApisController` |
| `IApiLogService` | Registro de requests a `/api/` | `Middleware/ApiLoggingMiddleware` |
| `IAuthService` | Autenticación JWT para API consumers | `Api/AuthController` |
| `ICaptchaService` | Verificación Turnstile/reCAPTCHA | `LoginController`, `HomeController` |
| `IPublicPortalService` | Data read-only páginas públicas | `Public/HomeController` |
| `IInscriptionLookupService` | Endpoints AJAX públicos (check-user, type-modalities, careers, schools, etc.) | `Public/HomeController` |
| `IInscriptionService` | Alta inscripción pública (transacción EF + uploads + duplicate check + apoderado si menor de edad) | `Public/HomeController` |
| `IBrochureService` | CRUD de brochures/folletos | `BrochuresController` |
| `IFaqService` | CRUD de preguntas frecuentes | `FaqController` |
| `IInscriptionDocumentService` | Subida de documentos del postulante en el portal público | `InscriptionDocumentController` |
| `IFingerprintService` | Mock de servicio biométrico (a sustituir por bridge real) | — |
| `IConstanciaIngresoPdfRenderer` | Render PDF de constancia de ingreso (QuestPDF) | `DocumentIssuanceService` |
| `IDocumentService` | Servicio de documentos (Scriban + PuppeteerSharp) | `DocumentIssuanceService` |

**Ejemplo de uso (Faculty CRUD):**

```csharp
// Antes — controller con AppDbContext y lógica inline
public async Task<IActionResult> Index()
{
    var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
    return View(faculties);
}

// Después — solo HTTP, la consulta vive en el servicio
public async Task<IActionResult> Index(CancellationToken ct)
{
    var list = await _faculties.GetAllAsync(ct);
    return View(list);
}
```

### Estado de migración a servicios

La mayoría de controladores han sido migrados a servicios. Estado actual:

| Controlador | Servicio | Estado | Reducción |
|---|---|---|---|
| `Public/HomeController` | `IPublicPortalService` + `IInscriptionLookupService` + `IInscriptionService` | ✅ Completo | 1.300 → 353 (−73%) |
| `SchoolManagementController` | `ISchoolService` + `IUbigeoService` | ✅ Completo | 290 → 225 (−22%) |
| `ModalitiesController` | `IModalityService` + `ICatalogService` | ✅ Completo | 257 → 174 (−32%) |
| `CareersController` | `ICareerService` | ✅ Completo | |
| `FacultiesController` | `IFacultyService` | ✅ Completo | |
| `VacanciesController` | `IVacancyService` | ✅ Completo | 156 → 79 (−49%) |
| `TypeModalitiesController` | `ITypeModalityService` + `ICatalogService` | ✅ Completo | 239 → 164 (−31%) |
| `PaymentCodesController` | `IPaymentCodeService` + `ICatalogService` | ✅ Completo | 155 → 94 (−39%) |
| `PostulantsController` | `IPostulantQueryService` + `ICatalogService` + `IUbigeoService` | ✅ Completo | 331 → 207 (−37%) |
| `AttendanceController` | `IAttendanceService` | ✅ Completo | 200 → 97 (−52%) |
| `TematicAreasController` | `ITematicAreaService` + `ICatalogService` | ✅ Completo | 314 → 179 (−43%) |
| `UsersController` | `IUserManagementService` | ✅ Completo | 374 → 106 (−72%) |
| `BannersController` | `IBannerService` | ✅ Completo | 183 → 104 (−43%) |
| `ProspectsController` | `IProspectService` + `ICatalogService` | ✅ Completo | 183 → 127 (−31%) |
| `{Syllabi,Regulations,OtherFiles}` | `IOtherFilesService` | ✅ Completo | 513 → 327 (−36%) |
| `PublicInfosController` | `IPublicInfoService` + `ICatalogService` | ✅ Completo | 170 → 120 (−29%) |
| `ScheduleController` | `IScheduleEventService` + `ITermService` | ✅ Completo | 137 → 120 (−12%) |
| `TermsController` | `ITermService` | ✅ Completo | 264 → 87 (−67%) |
| `ExamProcessingController` | `IExamProcessingService` | ✅ Completo | 541 → |
| `AdminController` (dashboard) | `IDashboardService` | ✅ Completo | |
| `ReportController` (postulant resume) | `IPostulantResumeService` | ✅ Completo | |
| `TematicAreaReportController` | `ITematicAreaReportService` | ✅ Completo | |
| `ConfigController` | inyecta servicios | ✅ Completo | |
| `ProfileController` | `IProfileService` | ✅ Completo | |

**Cómo continuar:** copiar la plantilla de `IFacultyService`/`FacultyService`, ajustar el dominio, registrar en `Program.cs`, refactorizar el controlador. Para listados con filtros, heredar `ListQuery` y devolver `PagedResult<TItem>`.

---

## 8. Design System

El UI corre sobre **Tailwind 3.4** con paleta extendida y un set de utilidades CSS y partials Razor reutilizables. Todo el diseño es **sin gradientes** y soporta `html.dark` para futura activación.

### 8.1 Paleta y tipografía (`ADMISION/tailwind.config.js`)

| Token         | Uso                              | Valores                                              |
|---------------|----------------------------------|------------------------------------------------------|
| `primary`     | CTAs, énfasis, estados activos   | `#f54477` (DEFAULT) · escala 50–900                  |
| `secondary`   | Acciones secundarias, hero accent| `#716aca` (DEFAULT) · escala 50–900 · `1000`=`#0f172a` |
| `ink`         | Texto y superficies neutras      | 50 (`#f7f8fa`) – 950 (`#070912`)                     |
| `accent`      | Categorías y decoración          | `peach #ffb38a` · `mint #7be3c7` · `violet #c8a3ff` · `coral #ff8a8a` |
| `boxShadow`   | Sombras predefinidas             | `shadow-glow`, `shadow-glow-secondary`, `shadow-card`, `shadow-soft` |
| Fuentes       | `font-sans` / `font-serif`       | **Inter** (UI) · **Instrument Serif** (acentos italic) · **JetBrains Mono** (`kbd`, `font-mono`) |

Regla: **no `linear-gradient`/`radial-gradient`/`bg-gradient-*`** en backgrounds, botones, texto ni stripes. Usa colores sólidos + opacidad cuando necesites variantes (`bg-primary-500/15`, etc.).

### 8.2 Utilidades CSS (`ADMISION/wwwroot/css/site.css`)

Clases de presentación disponibles en cualquier vista:

| Categoría    | Clases                                                                                   |
|--------------|------------------------------------------------------------------------------------------|
| Superficies  | `.glass`, `.glass-strong`, `.ring-soft`, `.dotgrid`, `.mesh` (neutral)                   |
| Animación    | `.fade-up` + `.is-in`, `.lift`, `.pulse-dot`, `.shine`, `.skeleton`, `.sparkpath`, `.view-in`, `.toast-in` |
| Botones      | `.btn-grad` (primary sólido), `.btn-grad-secondary` (violeta sólido), `.btn-soft`        |
| Typography   | `.eyebrow`, `.gtext` (acento sólido)                                                     |
| Header       | `.nav-sticky`, `.nav-underline`, `.nav-item` + `.nav-group` (sidebar admin)              |
| **Tabla**    | `table.atlas` + `th.sortable`                                                            |
| **Badges**   | `.badge.b-{green,amber,red,violet,blue,gray,primary,secondary}`                          |
| **Controles**| `.seg` + `.seg button.on`, `.check` + `.on`, `.pbar`, `.chip` + `.active`                |
| **Forms**    | `.form-label`, `.form-input`, `.form-textarea`, `.form-date`, `.form-helper`, `.form-error`, `.form-input-wrapper`, `.form-input-icon`, `.input-group` + `.input-group__addon` |
| **Check/Switch** | `.form-check` (+ `.form-check--card`, `.form-check__text`, `.form-check__hint`), `.form-switch` |
| **Modal**    | `.adm-modal` + `.adm-modal__overlay` / `__card` / `__header` / `__title` / `__body` / `__footer` / `__close` — el card hace flex-column con `max-height: calc(100vh - 48px)`; **header/footer pegajosos** y **scroll interno** en `__body` (cualquier modal alto se desplaza dentro del card sin desbordar la pantalla). API global `window.ADM.Modal` cargada desde `wwwroot/js/admin/modal-api.js` (no requiere el partial `_Modal` — se pueden escribir modales inline con `<div class="adm-modal">`) |
| **Combobox** | `.combobox` + `.combobox__dropdown` / `__list` / `__option`                              |
| Misc         | `[data-tip]` (tooltip), `kbd`, `.dragdots`, `.cmd-row` + `.cmd-ico`, `.tl-progress`      |

### 8.3 Partials Razor reutilizables (`ADMISION/Pages/Shared/_*.cshtml`)

| Partial                | Descripción                                                                                  |
|------------------------|----------------------------------------------------------------------------------------------|
| `_DataTable`           | Tabla genérica con sort, paginación, badges, progress, skeleton y popover de acciones        |
| `_CustomSelect`        | Select con búsqueda; dropdown en `position: fixed` (escapa overflow-hidden)                  |
| `_CustomDropzone`      | Subida de archivos drag & drop                                                               |
| `_Modal`               | Modal genérico con `data-modal-open` / `data-modal-close` + API `ADM.Modal.open/close`       |
| `_FormInput`           | Input tipado (text/number/email/tel/password/search/url) con icon, addon, error              |
| `_FormDate`            | Calendario estilizado (Flatpickr) — locale ES, soporta `LinkedMinId`/`LinkedMaxId` para rangos |
| `_FormCheckbox`        | Checkbox / radio / switch estilizados — variantes `default`, `card`, `switch`                |
| `_FormTextarea`        | Textarea con contador de caracteres opcional                                                 |
| `_SearchInput`         | Input de búsqueda con icono, debounce y atajo de teclado                                     |
| `_Combobox`            | Input editable con dropdown filtrable (typeahead, permite texto libre opcionalmente)         |
| `_PageHero`            | Encabezado estándar (eyebrow + h1 + subtítulo + breadcrumbs + slot de acciones)              |
| `_FilterBar`           | Contenedor para search + filtros + botón "Limpiar"                                           |

#### Ejemplos rápidos

```cshtml
@* Modal *@
<partial name="_Modal" model='new {
    Id      = "deleteModal",
    Eyebrow = "Confirmación",
    Title   = "¿Eliminar registro?",
    Size    = "md",
    Body    = "<p class=\"text-sm text-ink-600\">Esta acción no se puede deshacer.</p>",
    Footer  = "<button class=\"btn-soft px-4 py-2 rounded-lg\" data-modal-close>Cancelar</button>" +
              "<button class=\"btn-grad px-4 py-2 rounded-lg\" id=\"confirmDelete\">Eliminar</button>"
}' />
<button data-modal-open="deleteModal">Eliminar</button>

@* Input con icono (los iconos usan el namespace Tabler `ti ti-*`) *@
<partial name="_FormInput" model='new {
    Id = "email", Name = "Email", Type = "email",
    Label = "Correo institucional", Required = true,
    IconLeft = "ti-mail", Helper = "Te enviaremos un código de verificación."
}' />

@* Input con prefijo "S/" *@
<partial name="_FormInput" model='new {
    Id = "amount", Name = "Amount", Type = "number",
    Label = "Monto", AddonLeft = "S/", Step = "0.01", Min = "0"
}' />

@* Combobox con sugerencias *@
<partial name="_Combobox" model='new {
    Id = "schoolPicker", Name = "School",
    Label = "Colegio",
    Options = colegios.Select(c => new { id = c.Id.ToString(), name = c.Name }),
    AllowFreeText = true
}' />

@* Fecha (acepta DateOnly / DateTime / string). Abre un calendario Flatpickr. *@
<partial name="_FormDate" model='new {
    Id = "StartDate", Name = "StartDate",
    Label = "Fecha de inicio", Required = true,
    Value = Model.StartDate,
    Min = "2026-01-01"
}' />

@* Par inicio/fin: la fecha "fin" no permite anteriores a "inicio". *@
<partial name="_FormDate" model='new {
    Id = "EndDate", Name = "EndDate",
    Label = "Fecha de fin", Required = true,
    Value = Model.EndDate,
    LinkedMinId = "StartDate"
}' />

@* Fecha + hora *@
<partial name="_FormDate" model='new {
    Id = "scheduledAt", Name = "ScheduledAt",
    Label = "Programado para", Type = "datetime-local",
    EnableTime = true
}' />

@* Checkbox: variante default *@
<partial name="_FormCheckbox" model='new {
    Id = "isActive", Name = "IsActive",
    Label = "Periodo activo",
    Hint  = "Visible para los postulantes",
    Checked = true
}' />

@* Switch (toggle iOS) *@
<partial name="_FormCheckbox" model='new {
    Id = "enabled", Name = "Replication.Enabled",
    Label = "Activar replicación",
    Variant = "switch"
}' />

@* Checkbox-tarjeta (lista de opciones tipo settings) *@
<partial name="_FormCheckbox" model='new {
    Id = "optModalities", Name = "Replication.Modalities",
    Label = "Modalidades",
    Hint  = "Incluye tipos, vacantes y requisitos.",
    Checked = true,
    Variant = "card"
}' />

@* Page hero con acento serif italic *@
<partial name="_PageHero" model='new {
    Eyebrow     = "Configuración académica",
    Title       = "Periodos académicos",
    TitleAccent = "académicos",
    Subtitle    = "Administra los ciclos de admisión.",
    Breadcrumbs = new[] { new { Label = "Periodos", Href = (string)null } },
    ActionsHtml = "<a href=\"/admin/periodos/crear\" class=\"btn-grad px-4 py-2.5 rounded-xl text-sm font-semibold\">Nuevo periodo</a>"
}' />
```

### 8.4 Animación `.fade-up`

Los layouts (`_PublicLayout`, `_AdminLayout`) montan un `IntersectionObserver` al cargar la página. Cualquier elemento con la clase `.fade-up` se anima al entrar al viewport. En admin, el observer usa `#main-content` como `root` porque el scroll vive ahí (no en el `window`).

Para re-animar después de inyectar HTML (AJAX, swap de vistas): `window.ADM.refreshFadeUp()`.

### 8.5 Librerías externas usadas en UI

Ya están cargadas en los layouts (vía CDN/NuGet local) — no se requieren instalaciones adicionales:

| Librería          | Para qué                                  |
|-------------------|-------------------------------------------|
| **Tailwind 3.4**  | Build CSS (`npm run build:css`)           |
| **Tabler Icons 3.44** | Iconografía outline self-hosted en `wwwroot/lib/tabler-icons/` — clase `ti ti-*` |
| **SweetAlert2 11**| Modales de confirmación con cuenta regresiva |
| **Toastify**      | Notificaciones temporales en top-right    |
| **Flatpickr 4.6** | Calendario estilizado para `_FormDate` (locale ES + tema custom rosa) |
| **Chart.js 4.4**  | Charts (dashboard/reportes) — disponible en `_AdminLayout` |
| **SignalR 8**     | Notificaciones en tiempo real             |
| **Inter Variable + Instrument Serif + JetBrains Mono** | Inter self-hosted (`wwwroot/lib/inter/` con `<link rel="preload">` en el layout para evitar FOUT); las otras dos vía Google Fonts |

---

