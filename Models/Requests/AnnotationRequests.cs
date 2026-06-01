using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Models.Requests;

public class AddHeaderFooterRequest
{
    public IFormFile? File { get; set; }
    public string? HeaderLeft { get; set; }
    public string? HeaderCenter { get; set; }
    public string? HeaderRight { get; set; }
    public bool HeaderShowPageNum { get; set; } = true;
    public bool HeaderShowDate { get; set; } = false;
    public string? FooterLeft { get; set; }
    public string? FooterCenter { get; set; }
    public string? FooterRight { get; set; }
    public bool FooterShowPageNum { get; set; } = true;
    public int HeaderHeightMm { get; set; } = 15;
    public int FooterHeightMm { get; set; } = 15;
}

public class AddTextWatermarkRequest
{
    public IFormFile? File { get; set; }
    public string Text { get; set; } = "CONFIDENTIAL";
    public int FontSizePt { get; set; } = 60;
    public string Color { get; set; } = "#C0C0C0";
    public double Opacity { get; set; } = 0.3;
    public double Rotation { get; set; } = -45;
    public bool AllPages { get; set; } = true;
}

public class AddImageWatermarkRequest
{
    public IFormFile? PdfFile { get; set; }
    public IFormFile? ImageFile { get; set; }
    public double Opacity { get; set; } = 0.3;
    public double Rotation { get; set; } = 0;
    public bool AllPages { get; set; } = true;
}

public class AddAnnotationRequest
{
    public IFormFile? File { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Author { get; set; } = "IronPDF Demo";
    public double X { get; set; } = 10;
    public double Y { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
    public string Color { get; set; } = "#FFFF00";
}

public class AddBookmarkRequest
{
    public IFormFile? File { get; set; }
    public List<BookmarkItem> Bookmarks { get; set; } = new();
}

public class BookmarkItem
{
    public string Text { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int ParentIndex { get; set; } = -1;
}
