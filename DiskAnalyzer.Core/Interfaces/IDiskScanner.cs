using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Interfaces;

public interface IDiskScanner
{
    Task<FolderNode> ScanAsync(string path, IProgress<ScanProgress> progress, CancellationToken cancellationToken);
    event EventHandler<ScanProgressEventArgs> ProgressChanged;
}
