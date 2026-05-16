using DiskAnalyzer.Core.Helpers;

namespace DiskAnalyzer.Core.Models;

public abstract class FileSystemNode
{
    public string Name { get; }
    public string FullPath { get; }
    public long SizeBytes { get; protected set; }
    public DateTime LastModified { get; }
    public FileSystemNode? Parent { get; }
    public double PercentOfParent { get; set; }
    public double PercentOfRoot { get; set; }
    public string FormattedSize => SizeFormatter.Format(SizeBytes);
    public abstract bool IsDirectory { get; }

    protected FileSystemNode(string name, string fullPath, long sizeBytes, DateTime lastModified, FileSystemNode? parent)
    {
        Name = name;
        FullPath = fullPath;
        SizeBytes = sizeBytes;
        LastModified = lastModified;
        Parent = parent;
    }
}
