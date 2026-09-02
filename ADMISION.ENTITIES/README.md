# ADMISION.ENTITIES — Capa de Dominio y Persistencia

**Class library** que contiene el modelo de dominio, el contexto de Entity Framework Core, las migraciones y las constantes del Sistema de Admisión.

---

## Estructura

```
ADMISION.ENTITIES/
├── Constants/
│   ├── AppConstants.cs       ← Roles (SuperAdmin, Admin, Soporte…),
│   │                            estados de inscripción, tipos de archivo,
│   │                            fases de cronograma, helper SelectOption
│   └── ConfigGeneral.cs      ← Claves de configuración del sistema
│                                (nombre institución, redes sociales, logo UNAMAD)
├── Data/
│   └── AppDbContext.cs       ← DbContext principal (~55 DbSet<>)
├── Migrations/               ← Migraciones EF Core
└── Models/                   ← Entidades agrupadas por subdominio (16 carpetas, ~74 archivos)
```

---

## Modelos por subdominio

### Api
`ApiRequestLog`, `ApiToken` — registro de consumo de APIs externas y tokens JWT.

### Biometrics
`Fingerprint`, `PostulantPhoto`, `PostulantAttendance` — datos biométricos (huellas, fotos, asistencia).

### DocumentaryManagement
`AcademicYearName`, `DocumentHeaderConfig`, `DocumentIssued`, `DocumentType` — gestión documental (tipos de documento, encabezados, emisión de constancias).

### EconomicManagement
`MethodPayment`, `PaymentCode`, `PaymentCodeModality`, `Payments` — gestión económica (códigos de pago, métodos, pagos registrados).

### Exam
`ExamSession`, `ExamParameters`, `ExamAreaConfig`, `ExamAnswerKey`, `PostulantAnswerSheet`, `PostulantAnswer`, `ExamScoreResult` — sesiones de examen, configuración por área, clave de respuestas, hojas de respuesta y resultados.

### Info
`Banner`, `Brochure`, `FaqItem`, `OtherFiles`, `Prospect`, `PublicInfo`, `University` — información pública del portal (banners, folletos, FAQ, prospectos, archivos varios).

### Infrastructure
`Classroom`, `Pavilion`, `ExamAssignment` — infraestructura (pabellones, salones, asignación de examen).

### Integrations
`ExternalApi`, `ApiQueryLog`, `ExternalAcademicInfo`, `ExternalPaymentDetail`, `ExternalPaymentVoucher` — integraciones con APIs externas (consultas RENIEC, servicios de pago, etc.).

### Modality
`Career`, `CareerImage`, `Faculty`, `Modality`, `ModalityCareer`, `Term`, `TypeModality`, `TypeModalityCareer`, `Vacancies`, `Beneficiarie`, `TematicArea`, `TematicAreaCareer`, `ScheduleEvent`, `ExamResult` — núcleo académico (carreras, facultades, modalidades, periodos, vacantes, áreas temáticas, resultados de examen).

### Notifications
`Notification`, `NotificationView` — notificaciones del sistema para usuarios admin.

### Postulant
`Postulant`, `Inscription`, `Parent`, `Resignation`, `Observations`, `FileSubmission`, `DisabilityType`, `PostulantDisability`, `TypePostulantInscription` — postulantes, inscripciones y toda la información asociada.

### Requirement
`FileRequirementManagement`, `ModalityRequisite`, `TypePostulantRequisite` — requisitos documentales por modalidad y tipo de postulante.

### Schools
`Schools` — catálogo de colegios de procedencia.

### System
`Audit`, `AccessLog`, `Config` — auditoría de cambios, log de accesos y configuración dinámica del sistema.

### Ubigeo
`Country`, `Department`, `Provincie`, `Distrit` — ubigeo (países, departamentos, provincias, distritos).

### Users
`Users`, `Rols`, `UserRol`, `Teachers`, `Observations` — usuarios del sistema, roles, docentes.

---

## AppDbContext

- **Ubicación**: `Data/AppDbContext.cs` (~720 líneas)
- **50+ `DbSet<>`** cubriendo todos los subdominios
- **`OnModelCreating`** con configuración Fluent: relaciones, índices únicos, `DeleteBehavior.Restrict` por defecto (evita borrados en cascada), precisión de decimales para puntajes de examen

---

## Migraciones

| Migración | Fecha | Descripción |
|---|---|---|
| `InitialMigrate` | 2026-06-27 | Esquema inicial completo |
| `AddBrochureTable` | 2026-06-30 | Tabla Brochure |
| `AddApiJwtTables` | 2026-07-02 | ApiToken, ApiRequestLog para auth JWT |

---

## Constantes

### AppConstants.cs
- `Roles`: `Administrador`, `Soporte`, `SuperAdmin`, `Consultor`, `ApiConsumer`
- `Usuarios`: `Activo` (1), `Bloqueado` (2), `Inactivo` (3)
- `InscripcionState`: `Pendiente`, `Observado`, `Aprobado`, `Rechazado`, `Retirado`
- `FileExtensions.Allowed`: extensiones de archivo permitidas
- `OtherFileCategory`: categorías de archivos públicos (temarios, reglamentos, otros)
- `SchedulePhase`: fases del cronograma con iconos y orden

### ConfigGeneral.cs
Claves de configuración del sistema con valores por defecto para UNAMAD: nombre, dirección, teléfono, redes sociales, URL del logo, colores institucionales, URL del mapa.

---

## Dependencias

- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0 — driver PostgreSQL
- `Microsoft.EntityFrameworkCore.Design` 10.0.2 — tooling para migraciones
