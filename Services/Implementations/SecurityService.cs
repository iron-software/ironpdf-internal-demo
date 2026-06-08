using System.Diagnostics;
using IronPdf;
using IronPdf.Signing;
using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using IronPdfDemo.Services.Interfaces;

namespace IronPdfDemo.Services.Implementations;

public class SecurityService : ISecurityService
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(IFileStorageService storage, ILogger<SecurityService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<PdfOperationResult<PdfFileResponse>> PasswordProtectAsync(PasswordProtectRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            if (req.File is null)
                return PdfOperationResult<PdfFileResponse>.Failure("No file provided.");

            using var fs = File.Create(tempPath);
            await req.File.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);

            if (!string.IsNullOrWhiteSpace(req.OwnerPassword))
                doc.SecuritySettings.OwnerPassword = req.OwnerPassword;
            if (!string.IsNullOrWhiteSpace(req.UserPassword))
                doc.SecuritySettings.UserPassword = req.UserPassword;

            doc.SecuritySettings.AllowUserPrinting = req.AllowPrinting
                ? IronPdf.Security.PdfPrintSecurity.FullPrintRights
                : IronPdf.Security.PdfPrintSecurity.NoPrint;
            doc.SecuritySettings.AllowUserCopyPasteContent = req.AllowCopying;
            doc.SecuritySettings.AllowUserAnnotations = req.AllowAnnotations;
            doc.SecuritySettings.AllowUserFormData = true;

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "protected");
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = doc.BinaryData.Length,
                PageCount = doc.PageCount,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PasswordProtect failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> RemovePasswordAsync(RemovePasswordRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            if (req.File is null)
                return PdfOperationResult<PdfFileResponse>.Failure("No file provided.");

            using var fs = File.Create(tempPath);
            await req.File.CopyToAsync(fs, ct);
            fs.Close();

            PdfDocument doc;
            try
            {
                doc = PdfDocument.FromFile(tempPath, req.Password);
            }
            catch
            {
                return PdfOperationResult<PdfFileResponse>.Failure("Incorrect password or file is not encrypted.", "WRONG_PASSWORD");
            }

            doc.SecuritySettings.RemovePasswordsAndEncryption();
            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "unlocked");
            doc.Dispose();
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = new FileInfo(Path.Combine(_storage.GetOutputDirectory(), fileName)).Length,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemovePassword failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> SignPdfAsync(SignPdfRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPdf = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        var tempCert = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pfx");
        var outPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}_signed.pdf");
        try
        {
            if (req.PdfFile is null || req.CertificateFile is null)
                return PdfOperationResult<PdfFileResponse>.Failure("PDF file and certificate file are required.");

            using (var fs = File.Create(tempPdf)) await req.PdfFile.CopyToAsync(fs, ct);
            using (var fs = File.Create(tempCert)) await req.CertificateFile.CopyToAsync(fs, ct);

            var signature = new PdfSignature(tempCert, req.CertPassword)
            {
                SigningContact = req.ContactInfo,
                SigningLocation = req.Location,
                SigningReason = req.SigningReason
            };

            using var pdfToSign = PdfDocument.FromFile(tempPdf);
            pdfToSign.Sign(signature);
            pdfToSign.SaveAs(outPath);
            var bytes = await File.ReadAllBytesAsync(outPath, ct);
            var baseName = Path.GetFileNameWithoutExtension(req.PdfFile.FileName);
            var fileName = await _storage.SaveFileAsync(bytes, baseName, "signed");
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = bytes.Length,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignPdf failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            foreach (var t in new[] { tempPdf, tempCert, outPath })
                if (File.Exists(t)) File.Delete(t);
        }
    }

    public async Task<PdfOperationResult<bool>> VerifySignatureAsync(VerifySignatureRequest req, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            if (req.File is null)
                return PdfOperationResult<bool>.Failure("No file provided.");

            using var fs = File.Create(tempPath);
            await req.File.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);
            var signatures = doc.GetVerifiedSignatures();
            if (signatures.Count == 0)
                return PdfOperationResult<bool>.Success(false);

            var allValid = signatures.All(s => s.Valid);
            return PdfOperationResult<bool>.Success(allValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifySignature failed");
            return PdfOperationResult<bool>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
