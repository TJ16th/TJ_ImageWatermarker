using System.Drawing;
using System.Drawing.Imaging;

namespace WatermarkApp;

internal static class Program
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Drag and drop JPEG/PNG files onto WaterMarker.exe");
            return 1;
        }

        var baseDir = AppContext.BaseDirectory;
        var successCount = 0;
        var failureCount = 0;

        foreach (var inputPath in args)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException("Input file not found.", inputPath);
                }

                var ext = Path.GetExtension(inputPath);
                if (!SupportedExtensions.Contains(ext))
                {
                    throw new NotSupportedException($"Unsupported extension: {ext}");
                }

                var metadata = MetadataProvider.BuildMetadata(inputPath);
                var outputPath = OutputNaming.GetUniqueOutputPath(baseDir, Path.GetFileNameWithoutExtension(inputPath));

                using var source = new Bitmap(inputPath);
                using var output = WatermarkEngine.ApplyWatermark(source, metadata);
                output.Save(outputPath, ImageFormat.Png);

                Console.WriteLine($"OK: {Path.GetFileName(inputPath)} -> {Path.GetFileName(outputPath)}");
                successCount++;
            }
            catch (Exception ex)
            {
                failureCount++;
                Console.WriteLine($"FAILED: {inputPath}");
                Console.WriteLine($"  {ex.Message}");
                ErrorLogger.Log(inputPath, ex);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Completed. Success: {successCount}, Failed: {failureCount}");
        return failureCount == 0 ? 0 : 2;
    }
}

internal static class ErrorLogger
{
    private static readonly object Sync = new();

    public static void Log(string inputPath, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "watermark_error.log");
            var lines = new[]
            {
                "-----",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                $"Input: {inputPath}",
                ex.ToString()
            };

            lock (Sync)
            {
                File.AppendAllLines(logPath, lines);
            }
        }
        catch
        {
            // Ignore logging failures to keep main flow stable.
        }
    }
}
