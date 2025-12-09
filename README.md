🧾 PDF Generator Demo
📘 Overview
A C# console application that generates PDF documents from HTML templates using IronPDF.
The application provides a menu-driven interface to select templates, configure rendering options, and generate PDFs with different input types:

Plain HTML
HTML with CSS
HTML with JavaScript
📁 Project Structure
PdfGeneratorDemo/
├── Configuration/
│   └── GeneratorConfiguration.cs
│
├── Controllers/
│   └── DocumentGenerationController.cs
│
├── Models/
│   ├── GenerationResult.cs
│   ├── TemplateConfiguration.cs
│   ├── TemplateInfo.cs
│   └── TemplateResult.cs
│
├── Output/
│   └── (Generated PDF files)
│
├── Services/
│   ├── PdfGeneratorService.cs      ← Main IronPDF processing class
│   ├── TemplateAnalyzer.cs
│   └── TemplateManager.cs
│
├── Templates/
│   ├── CSS-HTML.html               ← HTML with Complex CSS
│   ├── JS-CSS.html                 ← HTML with JavaScript & CSS
│   └── Plain.html                  ← Plain HTML
│
├── Utilities/
│   └── HtmlHelper.cs
│
├── appsettings.json                ← Configuration & License Key
└── Program.cs                      ← Main entry point

🧩 Key Components
Main Entry Point

Program.cs — The main console entry point with a user-friendly menu interface.

Template selection (Plain HTML, CSS HTML, JS HTML, or All)

Input type configuration

Paper orientation settings

License key management via appsettings.json

Core IronPDF Processing Class

Services/PdfGeneratorService.cs — The main class that handles the IronPDF rendering process.

Manages PDF generation from HTML content using ChromePdfRenderer

Configures rendering options (JavaScript execution, CSS media types, paper orientation)

Implements error handling to continue processing even if individual PDFs fail

Applies user-selected settings for optimal PDF output

⚙️ Core Features
Feature	Description
Template Selection	Process individual templates or all templates at once
Flexible Rendering	Configure JavaScript execution, CSS media types, and page orientation
Error Resilience	Continues processing remaining templates even if one PDF fails
Configuration Management	License key stored securely in appsettings.json
Multiple Output Options	Support for Portrait and Landscape orientations
🧱 Sample Templates

The application includes three sample HTML templates located in /Templates:

Template	Description
Plain.html	Basic HTML invoice with simple tables
CSS-HTML.html	Styled company report with complex CSS and gradients
JS-CSS.html	Interactive dashboard with JavaScript functionality
🚀 Usage
1️⃣ Configuration

Add your IronPDF license key to appsettings.json:

{
  "IronPdf": {
    "LicenseKey": "YOUR_LICENSE_KEY_HERE"
  }
}

2️⃣ Running the Application

Build and run the application:

dotnet build
dotnet run


You’ll be prompted with menu options such as:

Select Input Type:
1. Plain HTML
2. HTML with CSS
3. HTML with JavaScript

Select Orientation:
1. Portrait
2. Landscape


Generated PDFs are automatically saved to the /Output folder.

3️⃣ Menu Options
Template Selection

Plain HTML Template (processes Plain.html)

HTML with CSS Template (processes CSS-HTML.html)

HTML with JavaScript Template (processes JS-CSS.html)

All Templates (processes all .html files in /Templates)

Input Type

Plain HTML: Basic rendering without JavaScript

HTML with CSS: Enhanced CSS media handling

HTML with JavaScript: Enables JavaScript execution for charts or animations

Paper Orientation

Portrait: Standard vertical layout

Landscape: Horizontal layout for wider content

🧰 Requirements

.NET 6.0 or later

IronPDF library

Valid IronPDF license key (optional for trial mode)

Install IronPDF via NuGet:

dotnet add package IronPdf

🧩 Error Handling

Individual PDF failures do not stop the overall process

Each error is logged with a clear message

A summary report displays total success and failure counts at the end

📦 Output

All generated PDFs are saved in the /Output folder

Files are automatically named after their source templates

A clear console summary is shown after processing:

✓ Generated: Plain.pdf
✓ Generated: CSS-HTML.pdf
✓ Generated: JS-CSS.pdf

Summary: 3 succeeded, 0 failed.

🧠 Summary

PDF Generator Demo demonstrates professional-grade PDF generation using IronPDF,
featuring modular architecture, configurable rendering, and robust error handling —
ideal for enterprise-level or automation-based document generation workflows.
