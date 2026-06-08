namespace WatermarkApp;

internal static class MetadataProvider
{
    public static WatermarkMetadata BuildMetadata(string inputPath)
    {
        return new WatermarkMetadata(
            File.GetCreationTime(inputPath),
            Environment.UserName,
            Path.GetFileName(inputPath));
    }
}

internal sealed record WatermarkMetadata(DateTime CreationTime, string UserName, string FileName)
{
    public string HeaderText => $"{CreationTime:yyyy-MM-dd HH:mm} / {UserName}";
}
