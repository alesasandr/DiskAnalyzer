using DiskAnalyzer.Core.Interfaces;

namespace DiskAnalyzer.Core.Services;

public class RealFileSystemProvider : IFileSystemProvider
{
    public IEnumerable<string> GetDirectories(string path) =>
        Directory.GetDirectories(path);

    public IEnumerable<string> GetFiles(string path) =>
        Directory.GetFiles(path);

    public long GetFileSize(string filePath) =>
        new FileInfo(filePath).Length;

    public FileAttributes GetAttributes(string filePath) =>
        File.GetAttributes(filePath);

    public bool CanAccess(string path)
    {
        try
        {
            Directory.GetFiles(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public DateTime GetLastWriteTime(string path) =>
        File.GetLastWriteTime(path);
}
