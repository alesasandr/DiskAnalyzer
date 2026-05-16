using System.Globalization;
using System.Windows.Data;
using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.WPF.Converters;

[ValueConversion(typeof(FileSystemNode), typeof(string))]
public class FileSystemNodeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FolderNode folder)
            return folder.IsAccessDenied ? "🔒" : "📁";
        if (value is FileNode file)
            return GetFileIcon(file.Extension);
        return "📄";
    }

    private static string GetFileIcon(string ext) => ext.ToLowerInvariant() switch
    {
        ".exe" or ".msi" => "⚙️",
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "🖼️",
        ".mp4" or ".avi" or ".mkv" or ".mov" => "🎬",
        ".mp3" or ".wav" or ".flac" or ".ogg" => "🎵",
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
        ".pdf" => "📕",
        ".doc" or ".docx" => "📝",
        ".xls" or ".xlsx" => "📊",
        ".txt" or ".log" => "📄",
        ".cs" or ".py" or ".js" or ".ts" or ".cpp" or ".h" => "💻",
        _ => "📄"
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
