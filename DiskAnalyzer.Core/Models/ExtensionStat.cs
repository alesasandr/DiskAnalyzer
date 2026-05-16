using DiskAnalyzer.Core.Helpers;

namespace DiskAnalyzer.Core.Models;

public class ExtensionStat
{
    public string Extension { get; }
    public int FileCount { get; }
    public long TotalBytes { get; }
    public string FormattedSize => SizeFormatter.Format(TotalBytes);
    public double PercentOfRoot { get; set; }

    public ExtensionStat(string extension, int fileCount, long totalBytes)
    {
        Extension = extension;
        FileCount = fileCount;
        TotalBytes = totalBytes;
    }
}
