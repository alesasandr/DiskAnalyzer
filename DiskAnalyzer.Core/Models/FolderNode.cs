using System.Collections.ObjectModel;

namespace DiskAnalyzer.Core.Models;

public class FolderNode : FileSystemNode
{
    public override bool IsDirectory => true;
    public bool IsAccessDenied { get; set; }
    public ObservableCollection<FileSystemNode> Children { get; } = new();

    public int FileCount => Children.Sum(c => c is FolderNode f ? f.FileCount : 1);
    public int FolderCount => Children.Sum(c => c is FolderNode f ? 1 + f.FolderCount : 0);

    public FolderNode(string name, string fullPath, DateTime lastModified, FileSystemNode? parent)
        : base(name, fullPath, 0, lastModified, parent)
    {
    }

    public void AddChild(FileSystemNode child)
    {
        Children.Add(child);
    }

    public void RecalculateSize()
    {
        SizeBytes = Children.Sum(c => c.SizeBytes);
    }
}
