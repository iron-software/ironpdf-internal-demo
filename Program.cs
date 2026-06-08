using IronPdf;
using IronPdfDemo.Configuration;
using IronPdfDemo.Hubs;
using IronPdfDemo.Middleware;
using IronPdfDemo.Services.Implementations;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
builder.Services.Configure<IronPdfOptions>(
    builder.Configuration.GetSection(IronPdfOptions.SectionName));
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<SignalROptions>(
    builder.Configuration.GetSection(SignalROptions.SectionName));

var ironPdfOpts = builder.Configuration
    .GetSection(IronPdfOptions.SectionName)
    .Get<IronPdfOptions>() ?? new IronPdfOptions();

// ── IronPDF global setup (once at startup) ────────────────────────────────────
if (!string.IsNullOrWhiteSpace(ironPdfOpts.LicenseKey))
    License.LicenseKey = ironPdfOpts.LicenseKey;

Installation.ChromeGpuMode = IronPdf.Engines.Chrome.ChromeGpuModes.Disabled;

// ── MVC + Razor Pages ─────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IronPDF Demo API",
        Version = "v1",
        Description = "Comprehensive IronPDF 2026.5.2 feature showcase — 31 endpoints across 7 controllers.",
        Contact = new OpenApiContact { Name = "Iron Software", Url = new Uri("https://ironpdf.com") }
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── HTTP context accessor (for URL building in FileStorageService) ────────────
builder.Services.AddHttpContextAccessor();

// ── Domain Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IConversionService, ConversionService>();
builder.Services.AddScoped<IManipulationService, ManipulationService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<IAnnotationService, AnnotationService>();
builder.Services.AddScoped<IMetadataService, MetadataService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddSingleton<IBatchProgressService, BatchProgressService>();

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<IronPdfHealthCheck>("ironpdf")
    .AddCheck<StorageHealthCheck>("storage");

// ── Form data size limit (50 MB) ─────────────────────────────────────────────
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024;
});

// ── CORS (open for demo) ──────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── ProblemDetails ────────────────────────────────────────────────────────────
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Ensure storage directories exist ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IFileStorageService>().EnsureDirectoriesExist();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

// Swagger always enabled — this is a demo app
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "IronPDF Demo API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "IronPDF 2026 Demo API";
});

// Skip HTTPS redirect in Development so localhost:5100 (plain HTTP) works
// without browser certificate warnings on dev machines
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseCors();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<BatchProgressHub>("/hubs/batch-progress");
app.MapHealthChecks("/health");

app.Logger.LogInformation(
    "IronPDF Demo started. Licensed: {Licensed}",
    License.IsLicensed);

app.Run();
