using IronPdfConverter.Services.Interfaces;
using IronPdfConverter.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Register services
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPdfConversionService, PdfConversionService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

// Initialize output directory
try
{
    using (var scope = app.Services.CreateScope())
    {
        var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        fileStorageService.EnsureOutputDirectoryExists();
        Console.WriteLine("Output directory initialized successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not initialize output directory: {ex.Message}");
}

Console.WriteLine("Application starting...");
app.Run();