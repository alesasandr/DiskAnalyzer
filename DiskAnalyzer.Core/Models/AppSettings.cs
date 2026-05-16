namespace DiskAnalyzer.Core.Models;

public class AppSettings
{
    public bool SkipSystemFolders { get; set; } = true;
    public bool SkipHiddenFiles { get; set; } = false;
    public int MaxDepthLimit { get; set; } = 0;
    public SortMode DefaultSortMode { get; set; } = SortMode.BySizeDescending;
    public string Theme { get; set; } = "Dark";
    public bool ShowFileExtensionStats { get; set; } = true;
    public string LastScannedPath { get; set; } = string.Empty;
}
