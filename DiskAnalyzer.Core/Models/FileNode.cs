namespace DiskAnalyzer.Core.Models;

public class FileNode : FileSystemNode
{
    public override bool IsDirectory => false;
    public string Extension => Path.GetExtension(Name).ToLowerInvariant();
    public string FormattedExtension => string.IsNullOrEmpty(Extension) ? "(no ext)" : Extension;

    public FileNode(string name, string fullPath, long sizeBytes, DateTime lastModified, FileSystemNode? parent)
        : base(name, fullPath, sizeBytes, lastModified, parent)
    {
    }
}
