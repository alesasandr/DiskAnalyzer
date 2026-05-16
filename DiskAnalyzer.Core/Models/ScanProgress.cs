namespace DiskAnalyzer.Core.Models;

public class ScanProgress
{
    public string CurrentPath { get; init; } = string.Empty;
    public int ScannedFolders { get; init; }
    public int ScannedFiles { get; init; }
    public long TotalBytesFound { get; init; }
}

public class ScanProgressEventArgs : EventArgs
{
    public ScanProgress Progress { get; }
    public ScanProgressEventArgs(ScanProgress progress) => Progress = progress;
}
