using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Services;

public static class CommandLineParser
{
    public static ParsedArguments Parse(string[] args)
    {
        string? path = null;
        int top = 20;
        SortMode sortMode = SortMode.BySizeDescending;
        int depth = 0;
        bool showExtensions = false;
        bool listDrives = false;
        string? exportFormat = null;
        string? outputFile = null;

        int i = 0;
        while (i < args.Length)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--drives":
                    listDrives = true;
                    break;
                case "--top" when i + 1 < args.Length:
                    top = int.TryParse(args[++i], out var t) ? t : 20;
                    break;
                case "--sort" when i + 1 < args.Length:
                    sortMode = args[++i].ToLowerInvariant() switch
                    {
                        "name" => SortMode.ByNameAscending,
                        "count" => SortMode.ByFileCountDescending,
                        _ => SortMode.BySizeDescending
                    };
                    break;
                case "--depth" when i + 1 < args.Length:
                    depth = int.TryParse(args[++i], out var d) ? d : 0;
                    break;
                case "--extensions":
                    showExtensions = true;
                    break;
                case "--export" when i + 1 < args.Length:
                    exportFormat = args[++i].ToLowerInvariant();
                    break;
                case "--output" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                default:
                    if (!arg.StartsWith("--") && path == null)
                        path = arg;
                    break;
            }
            i++;
        }

        return new ParsedArguments(path, top, sortMode, depth, showExtensions, listDrives, exportFormat, outputFile);
    }
}
