using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Interfaces;

public interface IDriveProvider
{
    IReadOnlyList<DriveItem> GetAvailableDrives();
}
