namespace DiskAnalyzer.Core.Models;

public record ParsedArguments(
    string? Path,
    int Top,
    SortMode SortMode,
    int Depth,
    bool ShowExtensions,
    bool ListDrives,
    string? ExportFormat,
    string? OutputFile
);
