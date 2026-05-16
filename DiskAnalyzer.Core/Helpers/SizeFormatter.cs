namespace DiskAnalyzer.Core.Helpers;

public static class SizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        if (bytes < 0) return "0.0 B";
        double value = bytes;
        int unitIndex = 0;
        while (value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }
        return $"{value:F1} {Units[unitIndex]}";
    }
}
