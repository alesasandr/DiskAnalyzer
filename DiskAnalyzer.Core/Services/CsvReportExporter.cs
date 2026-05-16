using System.Text;
using DiskAnalyzer.Core.Helpers;
using DiskAnalyzer.Core.Interfaces;
using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Services;

public class CsvReportExporter : IReportExporter
{
    public async Task ExportToCsvAsync(FolderNode root, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Path,Type,Size,SizeBytes,FileCount,PercentOfRoot");
        AppendCsvNode(root, sb);
        await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendCsvNode(FileSystemNode node, StringBuilder sb)
    {
        var type = node.IsDirectory ? "Folder" : "File";
        var fileCount = node is FolderNode f ? f.FileCount.ToString() : "1";
        sb.AppendLine($"\"{node.FullPath}\",{type},{node.FormattedSize},{node.SizeBytes},{fileCount},{node.PercentOfRoot:F2}");

        if (node is FolderNode folder)
            foreach (var child in folder.Children)
                AppendCsvNode(child, sb);
    }

    public async Task ExportToTextAsync(FolderNode root, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Disk Analysis Report — {root.FullPath}");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('-', 80));
        AppendTextNode(root, sb, "", true);
        await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendTextNode(FileSystemNode node, StringBuilder sb, string indent, bool isLast)
    {
        var connector = isLast ? "└── " : "├── ";
        var size = SizeFormatter.Format(node.SizeBytes).PadLeft(10);
        sb.AppendLine($"{indent}{connector}{node.Name,-40} {size}  {node.PercentOfRoot,6:F1}%");

        if (node is FolderNode folder)
        {
            var childIndent = indent + (isLast ? "    " : "│   ");
            var children = folder.Children.OrderByDescending(c => c.SizeBytes).ToList();
            for (int i = 0; i < children.Count; i++)
                AppendTextNode(children[i], sb, childIndent, i == children.Count - 1);
        }
    }
}
