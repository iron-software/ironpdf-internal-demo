using IronPdf;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IronPdfDemo.Pages;

public class IndexModel(IFileStorageService storage) : PageModel
{
    public int OutputFileCount { get; private set; }
    public bool IsLicensed { get; private set; }

    public void OnGet()
    {
        OutputFileCount = storage.ListOutputFiles().Count();
        IsLicensed = License.IsLicensed;
    }
}
