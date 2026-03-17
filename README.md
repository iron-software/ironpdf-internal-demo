# IronPdfConverter

A **cloud-ready PDF conversion platform** built with **ASP.NET Core**, **Razor Pages**, and **IronPDF**.

This project provides a **stream-based, production-safe API and UI** for converting multiple input formats into PDFs, merging documents, redacting sensitive text, and compressing files — without relying on shared file paths between client and server.

---

## ✨ Features

- HTML → PDF  
- URL → PDF  
- Image → PDF (PNG, JPG, JPEG, GIF, BMP)  
- DOCX → PDF  
- Batch file conversion (partial-success safe)  
- Merge multiple PDFs  
- PDF text redaction  
- PDF compression (structural)  
- Stream / byte-based APIs (cloud-native)  
- Razor Pages UI + REST API  
- Clean architecture (SOLID)  
- IronPDF license-aware initialization  

---

## 🏗 Architecture Overview

The project follows a **layered, production-oriented architecture**:

```
IronPdfConverter
│
├── Controllers        # REST API endpoints
├── Pages              # Razor Pages UI
├── Services
│   ├── Interfaces     # Stream/byte-based abstractions
│   └── Implementations
├── Models             # Request/response DTOs
├── wwwroot            # Static assets
├── Output             # Generated PDFs (runtime)
│   └── temp           # Internal temp files (DOCX handling)
└── Program.cs
```

### Key Design Principles

- No file paths exposed in public APIs  
- All conversions use `Stream` / `byte[]`  
- File system is an internal concern only  
- Partial-success batch processing  
- Unified rendering pipeline via `ChromePdfRenderer`  

---

## 🔌 Technology Stack

- .NET 8 / ASP.NET Core  
- Razor Pages  
- IronPDF  
- Bootstrap 5  
- C# (async APIs)  

---

## 🔐 IronPDF Licensing

Set your IronPDF license key in `appsettings.json`:

```json
{
  "IronPdf": {
    "LicenseKey": "YOUR-LICENSE-KEY"
  }
}
```

The application:
- Initializes the license on startup
- Logs license status (`IsLicensed`)
- Safely falls back if no license is provided

---

## 🚀 Running the Project

### Prerequisites

- .NET 8 SDK
- IronPDF license (trial or paid)

### Run locally

```bash
dotnet restore
dotnet run
```

Then open:

```
https://localhost:5001
```

---

## 🌐 API Endpoints

### Convert HTML to PDF

```http
POST /PdfConverter/convert-html
Content-Type: application/json
```

```json
{
  "htmlContent": "<h1>Hello PDF</h1>"
}
```

---

### Convert URL to PDF

```http
POST /PdfConverter/convert-url
Content-Type: application/json
```

```json
{
  "url": "https://example.com"
}
```

---

### Convert Multiple Files (Batch)

```http
POST /PdfConverter/convert-files
Content-Type: multipart/form-data
```

- Supports mixed file types
- Unsupported files do **not** stop the batch
- Each file returns its own result

Example response:

```json
{
  "success": true,
  "results": [
    {
      "fileName": "doc1.docx",
      "success": true,
      "outputFileName": "doc1_20250101_abc123.pdf"
    },
    {
      "fileName": "file.xyz",
      "success": false,
      "error": "File type '.xyz' is not supported."
    }
  ]
}
```

---

### Merge PDFs

```http
POST /PdfConverter/merge-pdfs
Content-Type: multipart/form-data
```

- Requires at least **2 PDF files**
- Accepts uploaded streams (no file paths)

---

### Download Generated PDF

```http
GET /PdfConverter/download/{fileName}
```

---

## 🧠 Design Decisions

### Why images are wrapped in HTML

Images are embedded as base64 inside HTML and rendered using `ChromePdfRenderer` to ensure:

- Consistent margins and DPI
- Predictable scaling
- A single rendering pipeline
- Easier future extension (headers, footers, watermarks)

This avoids branching logic between different renderers.

---

### Why streams instead of file paths

File paths are unsafe and impractical in distributed systems.

Streams allow:
- Cloud deployments
- Remote API clients
- In-memory processing
- Improved security (no path traversal)

All public services operate on `Stream` or `byte[]`.

---

### Batch conversion behavior

- Each file is processed independently
- Failures are isolated per file
- Results are fully observable

---

## 🧪 Supported Formats

| Format | Supported |
|------|----------|
| HTML | Yes |
| URL | Yes |
| PNG / JPG / JPEG | Yes |
| GIF / BMP | Yes |
| DOCX | Yes |
| PDF (merge) | Yes |
| Others | No (reported per file) |

---

## 🛡 Security Notes

- No arbitrary file paths accepted
- Filename sanitization applied
- JavaScript disabled during rendering (safe default)
- Temp files cleaned automatically

---

## 📈 Extensibility Ideas

- Azure Blob Storage / S3 integration
- Authentication & rate limiting
- Background job processing
- PDF/A or PDF/UA compliance
- Watermarks, headers, and footers
- Webhook callbacks

---

## 📄 License

This project uses **IronPDF**, which requires a valid license for production use.

See: https://ironpdf.com/licensing/
