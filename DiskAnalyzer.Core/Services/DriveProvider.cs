using DiskAnalyzer.Core.Interfaces;
using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Services;

public class DriveProvider : IDriveProvider
{
    public IReadOnlyList<DriveItem> GetAvailableDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new DriveItem(
                d.Name,
                d.VolumeLabel,
                d.DriveType.ToString(),
                d.TotalSize,
                d.AvailableFreeSpace))
            .ToList();
    }
}
