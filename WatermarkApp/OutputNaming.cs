namespace WatermarkApp;

internal static class OutputNaming
{
    public static string GetUniqueOutputPath(string outputDirectory, string inputBaseName)
    {
        var baseName = string.IsNullOrWhiteSpace(inputBaseName) ? "image" : inputBaseName;
        var firstCandidate = Path.Combine(outputDirectory, $"{baseName}_watermarked.png");
        if (!File.Exists(firstCandidate))
        {
            return firstCandidate;
        }

        for (var i = 1; i <= 9999; i++)
        {
            var candidate = Path.Combine(outputDirectory, $"{baseName}_watermarked_{i:000}.png");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("Could not allocate output filename after 9999 attempts.");
    }
}
