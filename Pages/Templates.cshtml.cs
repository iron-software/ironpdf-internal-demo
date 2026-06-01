using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IronPdfDemo.Pages;

public class TemplatesModel(ITemplateService templateService) : PageModel
{
    public List<TemplateInfo> Templates { get; private set; } = new();

    public void OnGet()
    {
        var descriptions = new Dictionary<string, (string category, string desc)>
        {
            ["Invoice"] = ("Billing", "Professional invoice with line items, tax calculation, and payment terms."),
            ["Report"] = ("Analytics", "Business analytics report with KPI cards, performance tables, and CSS bar charts."),
            ["Dashboard"] = ("Analytics", "Interactive JS dashboard with canvas charts and count-up KPI animations."),
            ["Certificate"] = ("Recognition", "Award certificate with decorative borders, SVG seal, and signature lines."),
            ["Resume"] = ("HR", "Two-column professional CV with skills progress bars and experience timeline."),
            ["Brochure"] = ("Marketing", "3-column A4 landscape marketing brochure with hero section and pricing table."),
            ["FormTemplate"] = ("Forms", "Fillable AcroForm with text, checkbox, radio, select, and textarea fields.")
        };

        Templates = templateService.GetAvailableTemplates()
            .Select(name => new TemplateInfo
            {
                Name = name,
                Category = descriptions.GetValueOrDefault(name).category ?? "General",
                Description = descriptions.GetValueOrDefault(name).desc ?? $"Generate a PDF using the {name} template."
            })
            .ToList();
    }
}

public record TemplateInfo
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
