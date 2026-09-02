# Plantillas de documentos

Esta carpeta contiene las plantillas HTML que `DocumentService` usa para
generar los PDF (constancias, certificados, oficios, etc.).

## Cómo se renderizan

1. El admin elige un **Tipo de Documento** (tabla `DocumentaryManagement.DocumentType`).
   Cada tipo apunta al nombre del archivo de plantilla en esta carpeta —
   campo `TemplateName` (sin la extensión `.html`).
2. `DocumentService` carga el HTML, mezcla los datos con
   [Scriban](https://github.com/scriban/scriban) y lo convierte a PDF con
   [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp).
3. Los datos del encabezado (nombre de institución, dependencia, oficina,
   logos, etc.) salen de `DocumentHeaderConfig` (la fila marcada `IsActive`).
   El nombre del año proviene de `AcademicYearName` (también `IsActive`).
4. El correlativo (si aplica) lo emite el servicio reservando una fila en
   `DocumentIssued` antes de renderizar.

## Variables siempre disponibles en la plantilla

| Variable                         | Origen                                |
|----------------------------------|---------------------------------------|
| `header.institution_name`        | `DocumentHeaderConfig.InstitutionName`|
| `header.dependency`              | `DocumentHeaderConfig.Dependency`     |
| `header.office_name`             | `DocumentHeaderConfig.OfficeName`     |
| `header.address`                 | `DocumentHeaderConfig.Address`        |
| `header.phone`                   | `DocumentHeaderConfig.Phone`          |
| `header.email`                   | `DocumentHeaderConfig.Email`          |
| `header.ruc`                     | `DocumentHeaderConfig.Ruc`            |
| `header.website`                 | `DocumentHeaderConfig.Website`        |
| `header.logo_url`                | `DocumentHeaderConfig.LogoUrl`        |
| `header.secondary_logo_url`      | `DocumentHeaderConfig.SecondaryLogoUrl`|
| `header.footer_text`             | `DocumentHeaderConfig.FooterText`     |
| `header.year_name`               | `AcademicYearName.Name` (activo)      |
| `header.year`                    | `AcademicYearName.Year` (activo)      |
| `director.name`                  | `Config["Director"]`                  |
| `director.commission_name`       | `Config["DirecctorComision"]`         |
| `correlative.value`              | Correlativo entero                    |
| `correlative.display`            | Correlativo formateado (`CI-000123/2026`)|
| `issue.issued_at`                | Fecha actual                          |
| `data.*`                         | Cualquier campo que pase el llamador  |

## Marca de agua

El parámetro `WatermarkText` en `DocumentOptions` aplica un overlay diagonal
con CSS sobre cada página. La plantilla **no necesita prepararla**: se
inyecta automáticamente al renderizar.

## Imágenes

- Las rutas que empiezan con `/img/...`, `/uploads/...` o `~/...` se
  resuelven a la carpeta `wwwroot` automáticamente y se sirven con `file://`.
- Las URLs absolutas (`https://...`) se mantienen tal cual.
- También puedes guardar imágenes propias de plantillas en la subcarpeta
  `assets/` y referenciarlas como `assets/firma.png`.
