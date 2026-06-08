# IronPDF Enhanced Demo

A comprehensive ASP.NET Core 8 showcase application for **IronPDF 2026.5.2**, rebuilt from the ground up with a domain-driven service architecture, 8 API controllers, 33 endpoints, real-time SignalR batch processing, and a full sidebar-nav Razor Pages UI with SPA-style page transitions (no blank flash between sections).

## Features at a Glance

| Capability | Details |
|---|---|
| PDF Conversion | HTML string, URL, image (JPG/PNG/TIFF), DOCX |
| Headers & Footers | HTML and text headers/footers with `{page}` / `{total-pages}` tokens |
| Watermarks | Text watermarks with opacity/rotation; image stamps |
| Digital Signatures | PFX-based signing + cryptographic verification |
| Password Protection | User + owner passwords; print, copy, edit permission flags |
| PDF Forms | Create from HTML, inspect fields, fill field values, flatten |
| Annotations | Sticky-note text annotations per page |
| Bookmarks | Nested bookmark tree |
| Metadata | Read/write Title, Author, Subject, Keywords, Creator, Producer |
| PDF/A Compliance | Convert to PDF/A-1B or PDF/A-3B archival format |
| Manipulation | Merge, split, rotate, compress images+struct, redact text |
| Thumbnails | First-page PNG preview at configurable DPI/width |
| Batch Processing | Multi-file conversion with real-time SignalR progress |
| Templates | 7 production-quality HTML templates (Invoice, Report, Dashboard, Certificate, Resume, Brochure, Form) |

## Tech Stack

- **Runtime**: .NET 8, ASP.NET Core 8
- **PDF Engine**: IronPDF 2026.5.2
- **Real-time**: Microsoft.AspNetCore.SignalR (v8)
- **API Docs**: Swashbuckle.AspNetCore 6.9.0 (Swagger UI at `/swagger`)
- **Health**: Microsoft.Extensions.Diagnostics.HealthChecks
- **UI**: Razor Pages, Bootstrap 5, Fetch API

## Project Structure

```
IronPdfDemo/
├── Configuration/          IronPdfOptions, StorageOptions, SignalROptions
├── Controllers/            8 thin API controllers (33 endpoints)
├── Hubs/                   BatchProgressHub (SignalR)
├── Middleware/             ExceptionMiddleware (RFC 7807 ProblemDetails)
├── Models/
│   ├── Common/             PdfOperationResult<T>, PdfMetadataInfo, PageInfo, BatchProgressUpdate
│   ├── Requests/           Strongly-typed request models per domain
│   └── Responses/          PdfFileResponse, ThumbnailResponse, FormFieldsResponse, BatchResultResponse
├── Pages/                  10 Razor Pages (Index, Conversion, Manipulation, Security,
│   │                       Forms, Annotations, Metadata, Templates, Batch, Error)
│   └── Shared/             _Layout.cshtml (sidebar + topbar + progress bar)
├── Services/
│   ├── Interfaces/         9 service contracts
│   └── Implementations/    ConversionService, ManipulationService, SecurityService,
│                           FormService, AnnotationService, MetadataService,
│                           TemplateService, FileStorageService, BatchProgressService,
│                           IronPdfHealthCheck, StorageHealthCheck
├── Templates/              7 HTML templates (Invoice, Report, Dashboard, Certificate,
│                           Resume, Brochure, FormTemplate)
├── wwwroot/                Static assets (CSS, JS)
├── appsettings.json
└── Program.cs
```

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An IronPDF license key (optional — trial mode works for evaluation; production output requires a key)

### Clone & run

```bash
git clone https://github.com/iron-software/ironpdf-internal-demo.git
cd ironpdf-internal-demo
dotnet run
```

The app starts on:
- HTTP: `http://localhost:5100`
- HTTPS: `https://localhost:7100`
- Swagger UI: `http://localhost:5100/swagger`
- Health check: `http://localhost:5100/health`

> **Tip:** In Development, HTTPS redirect is disabled so `http://localhost:5100` works without certificate warnings. Run `dotnet dev-certs https --trust` once if you'd rather use HTTPS.

### Configure your license key

The shipped `appsettings.json` has an **empty** `IronPdf.LicenseKey` on purpose — never commit a real key. Provide one through any of the following (most secure first):

**1. .NET user-secrets (recommended for local dev)**

```bash
cd IronPdfDemo
dotnet user-secrets init
dotnet user-secrets set "IronPdf:LicenseKey" "YOUR-KEY-HERE"
```

**2. Environment variable**

```bash
# bash / zsh
export IronPdf__LicenseKey="YOUR-KEY-HERE"
dotnet run

# PowerShell
$env:IronPdf__LicenseKey = "YOUR-KEY-HERE"
dotnet run
```

**3. Local override file** (already in `.gitignore`)

Create `appsettings.Local.json` next to `appsettings.json`:

```json
{
  "IronPdf": { "LicenseKey": "YOUR-KEY-HERE" }
}
```

…and reference it in `Program.cs` if you prefer file-based overrides (`builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true)`).

Without a key, IronPDF runs in trial mode (watermarked output) — perfectly fine for exploring the UI.

## API Endpoints

### Conversion — `/api/conversion`

| Method | Path | Description |
|---|---|---|
| POST | `/html-to-pdf` | HTML string → PDF |
| POST | `/url-to-pdf` | URL → PDF |
| POST | `/image-to-pdf` | Image file → PDF |
| POST | `/docx-to-pdf` | DOCX file → PDF |
| POST | `/batch` | Multi-file batch → PDFs (SignalR progress) |
| POST | `/thumbnail` | PDF page → PNG base64 |

### Manipulation — `/api/manipulation`

| Method | Path | Description |
|---|---|---|
| POST | `/merge` | Merge 2+ PDFs |
| POST | `/split` | Split at page N |
| POST | `/rotate` | Rotate pages |
| POST | `/compress` | Compress images + struct tree |
| POST | `/redact` | Redact text patterns |
| POST | `/page-info` | Page dimensions + rotation |

### Security — `/api/security`

| Method | Path | Description |
|---|---|---|
| POST | `/protect` | Add user/owner password + permissions |
| POST | `/remove-password` | Remove all passwords |
| POST | `/sign` | Apply digital signature (PFX) |
| POST | `/verify` | Verify digital signatures |

### Forms — `/api/forms`

| Method | Path | Description |
|---|---|---|
| POST | `/fields` | List all AcroForm fields |
| POST | `/fill` | Fill form field values |
| POST | `/flatten` | Flatten (bake) form fields |
| POST | `/create-from-html` | Create fillable form from HTML |

### Annotations — `/api/annotations`

| Method | Path | Description |
|---|---|---|
| POST | `/header-footer` | Add HTML header/footer |
| POST | `/watermark/text` | Add text watermark |
| POST | `/watermark/image` | Add image stamp |
| POST | `/annotate` | Add sticky-note annotation |
| POST | `/bookmarks` | Add bookmarks |

### Metadata — `/api/metadata`

| Method | Path | Description |
|---|---|---|
| POST | `/get` | Read PDF metadata |
| POST | `/set` | Write PDF metadata |
| POST | `/pdfa` | Convert to PDF/A |

### Templates — `/api/templates`

| Method | Path | Description |
|---|---|---|
| GET | `/list` | List available templates |
| POST | `/generate` | Render template to PDF |
| POST | `/preview` | Render template, return thumbnail |

## Special Endpoints

| Path | Description |
|---|---|
| `/swagger` | Swagger UI — interactive API explorer |
| `/health` | Health check (IronPDF + Storage) |
| `/hubs/batch-progress` | SignalR hub for batch progress |
| `/files/download/{filename}` | Download generated PDF by filename |

## Generated Files

Output PDFs are saved to `Output/` (relative to the project). The folder is created automatically by `FileStorageService.EnsureDirectoriesExist()` at startup. Files are retained for 7 days by default (`Storage.RetainOutputDays` in `appsettings.json`).

Temp files used during processing are saved to `Temp/` and cleaned up immediately after each request.

> Both `Output/` and `Temp/` are listed in `.gitignore` — they are runtime artifacts, not source. Wiping them between runs is safe; the app will recreate them on start.

## UI / Transitions

The sidebar nav uses **SPA-style page swaps** instead of full browser navigations. Clicking any of the sidebar sections (Conversion / Editing / Security & Forms / Advanced) keeps the sidebar and topbar perfectly fixed while only the right-hand content cross-fades.

Implementation lives in `wwwroot/js/site.js` and `wwwroot/css/site.css`:

- `spaNavigate()` intercepts internal links, `fetch()`es the target page, parses it with `DOMParser`, and replaces only the `.page-body` element.
- `document.startViewTransition()` provides the cross-fade where supported (Chrome / Edge 111+). The CSS `::view-transition-old/new(root)` keyframes define the easing.
- A slim top-of-page progress bar (`#navProgress`) covers the fetch latency.
- Bootstrap tab panes inside each page use `.fade.show` for sub-tab transitions; the easing is overridden to a 6 px lift + 220 ms ease-out for a more refined feel.
- All animations are guarded by `prefers-reduced-motion: reduce`.

External / `_blank` targets (Swagger UI, Health Check) are left to native browser navigation.

## Configuration Reference

```json
{
  "IronPdf": {
    "LicenseKey": "",
    "DefaultTimeoutSeconds": 60,
    "EnableJavaScriptByDefault": true,
    "DefaultPaperSize": "A4",
    "DefaultMarginMm": 15
  },
  "Storage": {
    "OutputDirectory": "Output",
    "TempDirectory": "Temp",
    "MaxFileSizeMb": 50,
    "RetainOutputDays": 7,
    "EnableAutoCleanup": true
  },
  "SignalR": {
    "BatchProgressHubPath": "/hubs/batch-progress"
  }
}
```

## Architecture Notes

- All domain services are registered **Scoped** (IronPDF objects are not thread-safe).
- `BatchProgressService` is **Singleton** — holds `IHubContext<BatchProgressHub>`.
- `PdfOperationResult<T>` is the return type for all service methods — discriminated union of success/failure with `IsSuccess`, `Data`, `Error`, `ErrorCode`, and `ElapsedMs`.
- `ExceptionMiddleware` catches all unhandled exceptions and returns RFC 7807 `ProblemDetails` JSON with a trace ID.
- The `FormOptions.MultipartBodyLengthLimit` is set to 50 MB to accommodate large PDF uploads.
- Swagger is enabled unconditionally (this is a demo app).

## Templates

| Template | Use Case | JS Required |
|---|---|---|
| `Invoice.html` | Professional invoice with line items | No |
| `Report.html` | Business report with KPI cards and bar chart | No |
| `Dashboard.html` | Interactive dashboard with canvas chart | Yes (WaitFor 1 s) |
| `Certificate.html` | Award certificate with SVG seal | No |
| `Resume.html` | Two-column CV with skills bars | No |
| `Brochure.html` | A4 landscape 3-column marketing brochure | No |
| `FormTemplate.html` | Fillable AcroForm (text, checkbox, radio, select) | No |

Token replacement: `{{TOKEN_NAME}}` in templates is replaced at render time via `TemplateService`.

## Feature Comparison vs Original Demo

| Feature | Original (2025.10.8) | This Demo (2026.5.2) |
|---|---|---|
| IronPDF version | 2025.10.8 | **2026.5.2** |
| Headers & Footers | No | Yes |
| Watermarks | No | Yes (text + image) |
| Digital Signatures | No | Yes |
| PDF Forms | No | Yes (create/fill/flatten) |
| Metadata read/write | No | Yes |
| Password Protection | No | Yes (owner + user + perms) |
| Bookmarks | No | Yes |
| Annotations | No | Yes |
| Page Rotation | No | Yes |
| Split PDF | No | Yes |
| PDF/A Compliance | No | Yes (A-1B + A-3B) |
| Thumbnails | No | Yes |
| Real-time progress | No | Yes (SignalR) |
| Swagger/OpenAPI | No | Yes |
| Health checks | No | Yes |
| Error handling | Basic | RFC 7807 ProblemDetails |
| Templates | 3 | 7 |
| Services | 1 monolithic | 7 domain services |
| API endpoints | ~8 | **33** |
| Razor Pages | Single-page tabs | 9 dedicated pages |
