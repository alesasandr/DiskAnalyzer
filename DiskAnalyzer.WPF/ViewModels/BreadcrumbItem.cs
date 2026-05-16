using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.WPF.ViewModels;

public class BreadcrumbItem
{
    public string Name { get; }
    public FolderNode Node { get; }

    public BreadcrumbItem(string name, FolderNode node)
    {
        Name = name;
        Node = node;
    }
}
