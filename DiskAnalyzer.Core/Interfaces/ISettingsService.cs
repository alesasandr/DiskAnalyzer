using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.Core.Interfaces;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
