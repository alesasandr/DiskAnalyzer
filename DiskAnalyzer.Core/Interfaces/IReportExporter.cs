using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Interfaces;

public interface IReportExporter
{
    Task ExportToCsvAsync(FolderNode root, string outputPath);
    Task ExportToTextAsync(FolderNode root, string outputPath);
}
