using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Options;

namespace SulthanERP.Api.Printing;

public sealed record KitchenPrintDocument(
    int PrintJobId,
    string KitchenName,
    string PrinterName,
    string JobName,
    string Content);

public interface IKitchenPrintTransport
{
    Task PrintAsync(
        KitchenPrintDocument document,
        CancellationToken cancellationToken);
}

public sealed class KitchenPrintTransport : IKitchenPrintTransport
{
    private readonly IOptionsMonitor<KitchenPrintingOptions> _options;
    private readonly IWebHostEnvironment _environment;

    public KitchenPrintTransport(
        IOptionsMonitor<KitchenPrintingOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options;
        _environment = environment;
    }

    public async Task PrintAsync(
        KitchenPrintDocument document,
        CancellationToken cancellationToken)
    {
        var mode = _options.CurrentValue.Mode.Trim();
        if (string.Equals(mode, "File", StringComparison.OrdinalIgnoreCase))
        {
            await WriteTestFileAsync(document, cancellationToken);
            return;
        }

        if (!string.Equals(mode, "Windows", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Unsupported KitchenPrinting mode '{mode}'. Use File or Windows.");

        cancellationToken.ThrowIfCancellationRequested();
        WindowsRawPrinter.Print(document.PrinterName, document.JobName, document.Content);
    }

    private async Task WriteTestFileAsync(
        KitchenPrintDocument document,
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

        var safeKotName = SanitizeFileName(document.JobName);
        var fileName =
            $"{document.PrintJobId:D6}_{safeKotName}_{SanitizeFileName(document.KitchenName)}.txt";
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

internal static class WindowsRawPrinter
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string DocumentName = string.Empty;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? OutputFile;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string DataType = "RAW";
    }

    public static void Print(
        string printerName,
        string documentName,
        string content)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "Windows printer mode can only run on Windows.");

        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("Kitchen printer name is not configured.");

        if (!OpenPrinter(printerName, out var printerHandle, IntPtr.Zero))
            throw CreatePrinterException(printerName, "open");

        try
        {
            var documentInfo = new DocInfo { DocumentName = documentName };
            if (StartDocPrinter(printerHandle, 1, documentInfo) == 0)
                throw CreatePrinterException(printerName, "start document");

            try
            {
                if (!StartPagePrinter(printerHandle))
                    throw CreatePrinterException(printerName, "start page");

                try
                {
                    var bytes = Encoding.UTF8.GetBytes(content + "\n\n\n\f");
                    if (!WritePrinter(
                            printerHandle,
                            bytes,
                            bytes.Length,
                            out var bytesWritten) ||
                        bytesWritten != bytes.Length)
                    {
                        throw CreatePrinterException(printerName, "write");
                    }
                }
                finally
                {
                    EndPagePrinter(printerHandle);
                }
            }
            finally
            {
                EndDocPrinter(printerHandle);
            }
        }
        finally
        {
            ClosePrinter(printerHandle);
        }
    }

    private static Win32Exception CreatePrinterException(
        string printerName,
        string operation)
    {
        return new Win32Exception(
            Marshal.GetLastWin32Error(),
            $"Could not {operation} kitchen printer '{printerName}'.");
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(
        string printerName,
        out IntPtr printerHandle,
        IntPtr defaults);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int StartDocPrinter(
        IntPtr printerHandle,
        int level,
        [In] DocInfo documentInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(
        IntPtr printerHandle,
        byte[] bytes,
        int byteCount,
        out int bytesWritten);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr printerHandle);
}
