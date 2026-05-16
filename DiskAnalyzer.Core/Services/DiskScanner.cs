using DiskAnalyzer.Core.Interfaces;
using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Services;

public class DiskScanner : IDiskScanner
{
    private readonly IFileSystemProvider _fs;
    private readonly AppSettings _settings;

    public event EventHandler<ScanProgressEventArgs>? ProgressChanged;

    private static readonly HashSet<string> SystemFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "$Recycle.Bin", "System Volume Information", "$WinREAgent",
        "Recovery", "boot", "EFI"
    };

    public DiskScanner(IFileSystemProvider fs, AppSettings settings)
    {
        _fs = fs;
        _settings = settings;
    }

    public async Task<FolderNode> ScanAsync(string path, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
    {
        int scannedFiles = 0;
        int scannedFolders = 0;
        long totalBytes = 0;

        var root = await Task.Run(() =>
        {
            var rootNode = ScanFolder(path, null, ref scannedFiles, ref scannedFolders, ref totalBytes,
                progress, cancellationToken, 0);
            return rootNode;
        }, cancellationToken);

        // Calculate percentages
        CalculatePercentages(root, root.SizeBytes);

        return root;
    }

    private FolderNode ScanFolder(string path, FolderNode? parent,
        ref int scannedFiles, ref int scannedFolders, ref long totalBytes,
        IProgress<ScanProgress> progress, CancellationToken ct, int depth)
    {
        ct.ThrowIfCancellationRequested();

        var dirInfo = new DirectoryInfo(path);
        var folder = new FolderNode(dirInfo.Name, path, dirInfo.LastWriteTime, parent);

        if (_settings.MaxDepthLimit > 0 && depth >= _settings.MaxDepthLimit)
            return folder;

        // Check access
        if (!_fs.CanAccess(path))
        {
            folder.IsAccessDenied = true;
            return folder;
        }

        // Scan files
        try
        {
            foreach (var filePath in _fs.GetFiles(path))
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var fileInfo = new FileInfo(filePath);

                    if (_settings.SkipHiddenFiles && (fileInfo.Attributes & FileAttributes.Hidden) != 0)
                        continue;

                    var fileNode = new FileNode(fileInfo.Name, filePath, fileInfo.Length, fileInfo.LastWriteTime, folder);
                    folder.AddChild(fileNode);
                    totalBytes += fileInfo.Length;
                    scannedFiles++;

                    if (scannedFiles % 100 == 0)
                    {
                        progress.Report(new ScanProgress
                        {
                            CurrentPath = path,
                            ScannedFiles = scannedFiles,
                            ScannedFolders = scannedFolders,
                            TotalBytesFound = totalBytes
                        });
                        ProgressChanged?.Invoke(this, new ScanProgressEventArgs(new ScanProgress
                        {
                            CurrentPath = path,
                            ScannedFiles = scannedFiles,
                            ScannedFolders = scannedFolders,
                            TotalBytesFound = totalBytes
                        }));
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
        catch (UnauthorizedAccessException)
        {
            folder.IsAccessDenied = true;
        }

        // Scan subdirectories
        try
        {
            foreach (var dirPath in _fs.GetDirectories(path))
            {
                ct.ThrowIfCancellationRequested();

                var name = Path.GetFileName(dirPath);

                if (_settings.SkipSystemFolders && SystemFolderNames.Contains(name))
                    continue;

                try
                {
                    var attrs = _fs.GetAttributes(dirPath);
                    if (_settings.SkipHiddenFiles && (attrs & FileAttributes.Hidden) != 0)
                        continue;
                }
                catch { }

                scannedFolders++;
                var subFolder = ScanFolder(dirPath, folder, ref scannedFiles, ref scannedFolders,
                    ref totalBytes, progress, ct, depth + 1);
                folder.AddChild(subFolder);
            }
        }
        catch (UnauthorizedAccessException)
        {
            folder.IsAccessDenied = true;
        }

        folder.RecalculateSize();
        return folder;
    }

    private static void CalculatePercentages(FolderNode node, long rootSize)
    {
        foreach (var child in node.Children)
        {
            child.PercentOfRoot = rootSize > 0 ? (double)child.SizeBytes / rootSize * 100.0 : 0;
            child.PercentOfParent = node.SizeBytes > 0 ? (double)child.SizeBytes / node.SizeBytes * 100.0 : 0;

            if (child is FolderNode subFolder)
                CalculatePercentages(subFolder, rootSize);
        }
    }
}
