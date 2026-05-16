using DiskAnalyzer.Core.Models;
using DiskAnalyzer.Core.Services;

var argsParsed = CommandLineParser.Parse(args);

if (argsParsed.ListDrives)
{
    ListDrives();
    return;
}

if (string.IsNullOrEmpty(argsParsed.Path))
{
    Console.WriteLine("Disk Analyzer — usage:");
    Console.WriteLine("  diskanalyzer <path>                   Scan and show top folders");
    Console.WriteLine("  diskanalyzer <path> --top N           Show top N folders");
    Console.WriteLine("  diskanalyzer <path> --sort name       Sort by name");
    Console.WriteLine("  diskanalyzer <path> --depth N         Limit scan depth");
    Console.WriteLine("  diskanalyzer <path> --extensions      Show extension stats");
    Console.WriteLine("  diskanalyzer <path> --export csv      Export to CSV");
    Console.WriteLine("  diskanalyzer --drives                 List available drives");
    return;
}

if (!Directory.Exists(argsParsed.Path))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Path not found: {argsParsed.Path}");
    Console.ResetColor();
    return;
}

Console.WriteLine($"Scanning {argsParsed.Path}...");

var settings = new AppSettings
{
    MaxDepthLimit = argsParsed.Depth,
    DefaultSortMode = argsParsed.SortMode
};

var scanner = new DiskScanner(new RealFileSystemProvider(), settings);

int lastFiles = 0;
var progress = new Progress<ScanProgress>(p =>
{
    if (p.ScannedFiles - lastFiles >= 500)
    {
        Console.Write($"\r  {p.ScannedFiles:N0} files, {p.ScannedFolders:N0} folders scanned...    ");
        lastFiles = p.ScannedFiles;
    }
});

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var sw = System.Diagnostics.Stopwatch.StartNew();
FolderNode root;
try
{
    root = await scanner.ScanAsync(argsParsed.Path, progress, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nCancelled.");
    return;
}
sw.Stop();

Console.WriteLine($"\r  {root.FileCount:N0} files, {root.FolderCount:N0} folders — {root.FormattedSize}          ");
Console.WriteLine();

PrintTree(root, argsParsed.Top, argsParsed.SortMode);

if (argsParsed.ShowExtensions)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("Extension Statistics:");
    Console.ResetColor();
    var stats = ExtensionAnalyzer.Analyze(root);
    Console.WriteLine($"  {"Extension",-12} {"Files",8}  {"Size",12}  {"% Total",8}");
    Console.WriteLine($"  {new string('-', 46)}");
    foreach (var s in stats.Take(20))
        Console.WriteLine($"  {s.Extension,-12} {s.FileCount,8:N0}  {s.FormattedSize,12}  {s.PercentOfRoot,7:F1}%");
}

if (!string.IsNullOrEmpty(argsParsed.ExportFormat))
{
    var exporter = new CsvReportExporter();
    var outFile = argsParsed.OutputFile ?? $"report.{argsParsed.ExportFormat}";
    if (argsParsed.ExportFormat == "csv")
        await exporter.ExportToCsvAsync(root, outFile);
    else
        await exporter.ExportToTextAsync(root, outFile);

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Exported to: {outFile}");
    Console.ResetColor();
}

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine($"Completed in {sw.Elapsed.TotalSeconds:F1}s");
Console.ResetColor();

static void ListDrives()
{
    var provider = new DriveProvider();
    var drives = provider.GetAvailableDrives();
    Console.WriteLine($"  {"Drive",-6} {"Label",-20} {"Type",-12} {"Total",10}  {"Free",10}  {"Used%",6}");
    Console.WriteLine($"  {new string('-', 70)}");
    foreach (var d in drives)
        Console.WriteLine($"  {d.Name,-6} {d.Label,-20} {d.DriveType,-12} {d.FormattedTotal,10}  {d.FormattedFree,10}  {d.UsedPercent,5:F1}%");
}

static void PrintTree(FolderNode root, int top, SortMode sort)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  {root.FullPath,-50} {root.FormattedSize,10}  100.0%");
    Console.ResetColor();

    var children = SortChildren(root.Children, sort).Take(top).ToList();
    for (int i = 0; i < children.Count; i++)
        PrintNode(children[i], "  ", i == children.Count - 1, sort, 1);
}

static void PrintNode(FileSystemNode node, string indent, bool isLast, SortMode sort, int depth)
{
    var connector = isLast ? "└── " : "├── ";
    Console.Write($"{indent}{connector}");
    if (node.IsDirectory) Console.ForegroundColor = ConsoleColor.Yellow;
    else Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write($"{node.Name,-40}");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($" {node.FormattedSize,10}");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  {node.PercentOfRoot,5:F1}%");
    Console.ResetColor();

    if (node is FolderNode folder && depth < 3)
    {
        var childIndent = indent + (isLast ? "    " : "│   ");
        var children = SortChildren(folder.Children, sort).Take(5).ToList();
        for (int i = 0; i < children.Count; i++)
            PrintNode(children[i], childIndent, i == children.Count - 1, sort, depth + 1);
    }
}

static IEnumerable<FileSystemNode> SortChildren(IEnumerable<FileSystemNode> items, SortMode sort) =>
    sort switch
    {
        SortMode.BySizeAscending => items.OrderBy(c => c.SizeBytes),
        SortMode.ByNameAscending => items.OrderBy(c => c.Name),
        SortMode.ByNameDescending => items.OrderByDescending(c => c.Name),
        SortMode.ByFileCountDescending => items.OrderByDescending(c => c is FolderNode f ? f.FileCount : 0),
        _ => items.OrderByDescending(c => c.SizeBytes)
    };
