using System.Text;
using Microsoft.Extensions.Options;

namespace SulthanERP.Api.Printing;

public sealed record CashierPrintDocument(
    int PrintJobId,
    string PrinterName,
    string JobName,
    string Content);

public interface ICashierPrintTransport
{
    Task PrintAsync(
        CashierPrintDocument document,
        CancellationToken cancellationToken);
}

public sealed class CashierPrintTransport : ICashierPrintTransport
{
    private readonly IOptionsMonitor<CashierPrintingOptions> _options;
    private readonly IWebHostEnvironment _environment;

    public CashierPrintTransport(
        IOptionsMonitor<CashierPrintingOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options;
        _environment = environment;
    }

    public async Task PrintAsync(
        CashierPrintDocument document,
        CancellationToken cancellationToken)
    {
        var mode = _options.CurrentValue.Mode.Trim();
        if (string.Equals(mode, "File", StringComparison.OrdinalIgnoreCase))
        {
            await WriteTestFileAsync(document, cancellationToken);
            return;
        }

        if (!string.Equals(mode, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported CashierPrinting mode '{mode}'. Use File or Windows.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        WindowsRawPrinter.Print(
            document.PrinterName,
            document.JobName,
            document.Content);
    }

    private async Task WriteTestFileAsync(
        CashierPrintDocument document,
        CancellationToken cancellationToken)
    {
        var configuredPath = _options.CurrentValue.SpoolDirectory;
        var rootPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(_environment.ContentRootPath, configuredPath);
        var outputPath = Path.Combine(
            rootPath,
            "Printed",
            DateTime.Now.ToString("yyyyMMdd"));

        Directory.CreateDirectory(outputPath);

        var fileName =
            $"{document.PrintJobId:D6}_{SanitizeFileName(document.JobName)}.txt";
        var finalPath = Path.Combine(outputPath, fileName);
        var temporaryPath = finalPath + ".tmp";

        await File.WriteAllTextAsync(
            temporaryPath,
            document.Content,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
        File.Move(temporaryPath, finalPath, overwrite: true);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var safeCharacters = value
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray();
        return new string(safeCharacters).Trim();
    }
}
