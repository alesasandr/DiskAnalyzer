using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Services;

public static class ExtensionAnalyzer
{
    public static IReadOnlyList<ExtensionStat> Analyze(FolderNode root)
    {
        var groups = new Dictionary<string, (int count, long bytes)>(StringComparer.OrdinalIgnoreCase);
        WalkTree(root, groups);

        long totalBytes = root.SizeBytes;
        var stats = groups.Select(kvp =>
        {
            var stat = new ExtensionStat(kvp.Key, kvp.Value.count, kvp.Value.bytes);
            stat.PercentOfRoot = totalBytes > 0 ? (double)kvp.Value.bytes / totalBytes * 100.0 : 0;
            return stat;
        })
        .OrderByDescending(s => s.TotalBytes)
        .ToList();

        return stats;
    }

    private static void WalkTree(FolderNode folder, Dictionary<string, (int, long)> groups)
    {
        foreach (var child in folder.Children)
        {
            if (child is FileNode file)
            {
                var ext = file.Extension;
                if (string.IsNullOrEmpty(ext)) ext = "(no ext)";

                if (groups.TryGetValue(ext, out var existing))
                    groups[ext] = (existing.Item1 + 1, existing.Item2 + file.SizeBytes);
                else
                    groups[ext] = (1, file.SizeBytes);
            }
            else if (child is FolderNode sub)
            {
                WalkTree(sub, groups);
            }
        }
    }

    public static IReadOnlyList<FileNode> GetTopLargestFiles(FolderNode root, int count = 50)
    {
        var files = new List<FileNode>();
        CollectFiles(root, files);
        return files.OrderByDescending(f => f.SizeBytes).Take(count).ToList();
    }

    private static void CollectFiles(FolderNode folder, List<FileNode> files)
    {
        foreach (var child in folder.Children)
        {
            if (child is FileNode file)
                files.Add(file);
            else if (child is FolderNode sub)
                CollectFiles(sub, files);
        }
    }
}
