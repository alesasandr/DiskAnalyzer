using DiskAnalyzer.Core.Helpers;

namespace DiskAnalyzer.Core.Models;

public class DriveItem
{
    public string Name { get; }
    public string Label { get; }
    public string DriveType { get; }
    public long TotalBytes { get; }
    public long FreeBytes { get; }
    public long UsedBytes => TotalBytes - FreeBytes;
    public double UsedPercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100.0 : 0;
    public string FormattedTotal => SizeFormatter.Format(TotalBytes);
    public string FormattedFree => SizeFormatter.Format(FreeBytes);
    public string FormattedUsed => SizeFormatter.Format(UsedBytes);
    public string DisplayName => $"{Name} {(string.IsNullOrWhiteSpace(Label) ? "" : $"[{Label}]")}".Trim();

    public DriveItem(string name, string label, string driveType, long totalBytes, long freeBytes)
    {
        Name = name;
        Label = label;
        DriveType = driveType;
        TotalBytes = totalBytes;
        FreeBytes = freeBytes;
    }
}
