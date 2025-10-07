# TicketBatchGenerator

A fast, parallel PDF ticket generator that reads passenger data from a CSV file, fills an HTML boarding pass template, and renders a **PDF per passenger** using [IronPDF](https://ironpdf.com/). Designed for bulk generation with progress logging and robust CSV parsing.

---

## TL;DR (Summary)
- **Input:** `flight_records.csv` (one passenger per row).
- **Output:** PDFs saved in `OutputTickets/` (e.g., `Jane_Doe_AA123_12A.pdf`).
- **Engine:** IronPDF `ChromePdfRenderer` (prewarmed per-thread for speed).
- **Scale:** Parallelized with `Parallel.ForEachAsync` using all CPU cores.
- **Safety:** HTML escaping + safe filenames; skips invalid/incomplete rows.

---

## Features
- 🚀 **Parallel bulk generation** with ETA & progress logs (every 20 tickets).
- 🧱 **Thread-local** `ChromePdfRenderer` to reduce contention and warm-up cost.
- 🧾 **Robust CSV parser** (supports quoted fields and embedded commas).
- 🧼 **XSS-safe HTML injection** and filesystem-safe PDF filenames.
- 🖨️ **Print-friendly layout** using CSS `@media print` profile and fixed viewport.

---

## Requirements
- **.NET**: .NET 6.0 or later.
- **OS**: Windows / Linux / macOS (IronPDF supports all major platforms).
- **Library**: [IronPDF](https://ironpdf.com/). Add via NuGet:
  ```bash
  dotnet add package IronPdf
  ```

---

## Project Structure
```
TicketBatchGenerator/
  Program.cs               # Entry point (stopwatch, logging, orchestration)
  TicketGenerator.cs       # Bulk & single-ticket generation
  CsvDataReader.cs         # Robust CSV parsing
  PassengerTicketData.cs   # Data model & validation
```
> (The above files are combined in your snippet; organize as you prefer.)

---

## CSV Schema

The app expects **9 columns** in `flight_records.csv` (header + rows).

| Column Index | Header             | Example        |
|--------------|--------------------|----------------|
| 0            | PassengerName      | Jane Doe       |
| 1            | FlightNumber       | AA123          |
| 2            | DepartureAirport   | JFK            |
| 3            | ArrivalAirport     | LAX            |
| 4            | FlightDate         | 2025-10-12     |
| 5            | DepartureTime      | 08:30          |
| 6            | CabinClass         | Economy        |
| 7            | SeatNumber         | 12A            |
| 8            | BarcodeData        | AA123|JFK|LAX  |

**Sample CSV**
```csv
PassengerName,FlightNumber,DepartureAirport,ArrivalAirport,FlightDate,DepartureTime,CabinClass,SeatNumber,BarcodeData
"Jane Doe",AA123,JFK,LAX,2025-10-12,08:30,Economy,12A,"AA123|JFK|LAX|20251012|0830|12A"
"John Smith",AA123,JFK,LAX,2025-10-12,08:30,Business,4C,"AA123|JFK|LAX|20251012|0830|4C"
```

---

## Configuration

Edit these values in `Program.Main` (or make them CLI args as needed):

```csharp
string csvFilePath = "\flight_records.csv";
string outputDirectory = "OutputTickets";
```

### IronPDF License (recommended)
Set your license via **environment variable** (preferred for secrets) or in code.

**Environment variable (recommended)**  
- Windows (PowerShell):
  ```powershell
  setx IronPdf.LicenseKey "IRONPDF-XXXX-XXXX-XXXX-XXXX"
  ```
- Linux/macOS (bash):
  ```bash
  export IronPdf_LicenseKey="IRONPDF-XXXX-XXXX-XXXX-XXXX"
  ```

**Optional code snippet** (add near app startup):
```csharp
// using IronPdf;
var key = Environment.GetEnvironmentVariable("IronPdf.LicenseKey") 
          ?? Environment.GetEnvironmentVariable("IronPdf_LicenseKey");
if (!string.IsNullOrWhiteSpace(key))
{
    License.LicenseKey = key;
}
```

> You can also place the key in `appsettings.json` and read it via `ConfigurationBuilder` if your app uses `Microsoft.Extensions.Configuration`.

---

## How It Works

1. **Load CSV** → `CsvDataReader.ReadPassengerData` parses rows safely (quoted values supported), skipping malformed lines with error logs.
2. **Warm renderer** → Thread-local `ChromePdfRenderer` instance per worker thread (reused for speed).
3. **Generate HTML** → Fill placeholders in `HtmlTemplate` with escaped values.
4. **Render PDF** → `RenderHtmlAsPdfAsync` creates a PDF document.
5. **Save file** → `PassengerName_FlightNumber_Seat.pdf` into `OutputTickets/`.
6. **Progress** → Logs processed count, elapsed time, and ETA every 20 files.
7. **Summary** → Prints total duration and average ms/ticket.

---

## Run

From the project directory with `Program.cs` compiled:

```bash
dotnet run --configuration Release
```

**Output**
```
Starting ticket generation at 2025-09-10 09:12:00
Loaded 250 passenger records from C:\...\flight_records.csv
Processed 20/250 tickets (Elapsed: 00:12, ETA: 02:08)
...
Ticket generation completed!
Files saved to: C:\path\to\OutputTickets
Total processing time: 03:01.442
Average time per ticket: 726 ms
```

---

## Notes & Tips
- **Performance**: Keep images/CSS simple; disable JavaScript (already disabled) to speed rendering.
- **Fonts**: Ensure required fonts are installed or embedded via CSS `@font-face`.
- **Stability**: Avoid unbounded HTML/JS; this template uses a **print-optimized** static layout.
- **Errors**: Parsing/Rendering exceptions are logged per row and don't abort the batch.

---

## Extending
- **CLI arguments**: Add `--csv` and `--out` switches to configure paths at runtime.
- **Branding**: Replace `HtmlTemplate` with a corporate-styled layout and logo.
- **Barcode**: Swap the text barcode placeholder for an actual barcode image or font.

---

## License
This project uses **IronPDF**. Ensure you have a valid license key. Other code is provided as-is under your chosen project license (e.g., MIT).
