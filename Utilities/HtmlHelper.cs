namespace PdfGeneratorDemo.Utilities
{
    public static class HtmlHelper
    {
        public static string EscapeHtml(string input)
        {
            return input?
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;") ?? string.Empty;
        }
    }
}
