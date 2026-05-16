namespace DiskAnalyzer.Core.Interfaces;

public interface IFileSystemProvider
{
    IEnumerable<string> GetDirectories(string path);
    IEnumerable<string> GetFiles(string path);
    long GetFileSize(string filePath);
    FileAttributes GetAttributes(string filePath);
    bool CanAccess(string path);
    DateTime GetLastWriteTime(string path);
}
