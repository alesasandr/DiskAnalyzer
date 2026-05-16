using DiskAnalyzer.Core.Interfaces;
using DiskAnalyzer.Core.Models;
using DiskAnalyzer.WPF.Commands;
using System.Windows.Input;

namespace DiskAnalyzer.WPF.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _original;
    private AppSettings _current;

    public bool SkipSystemFolders
    {
        get => _current.SkipSystemFolders;
        set { _current.SkipSystemFolders = value; OnPropertyChanged(); }
    }

    public bool SkipHiddenFiles
    {
        get => _current.SkipHiddenFiles;
        set { _current.SkipHiddenFiles = value; OnPropertyChanged(); }
    }

    public int MaxDepthLimit
    {
        get => _current.MaxDepthLimit;
        set { _current.MaxDepthLimit = value; OnPropertyChanged(); }
    }

    public string Theme
    {
        get => _current.Theme;
        set { _current.Theme = value; OnPropertyChanged(); }
    }

    public bool ShowFileExtensionStats
    {
        get => _current.ShowFileExtensionStats;
        set { _current.ShowFileExtensionStats = value; OnPropertyChanged(); }
    }

    public SortMode DefaultSortMode
    {
        get => _current.DefaultSortMode;
        set { _current.DefaultSortMode = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<string> Themes { get; } = ["Dark", "Light"];
    public IReadOnlyList<SortMode> SortModes { get; } = Enum.GetValues<SortMode>();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

    public bool? DialogResult { get; private set; }

    public event EventHandler? RequestClose;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _original = settingsService.Load();
        _current = CopySettings(_original);

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset);
    }

    private void Save()
    {
        _settingsService.Save(_current);
        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void Reset()
    {
        _current = new AppSettings();
        OnPropertyChanged(nameof(SkipSystemFolders));
        OnPropertyChanged(nameof(SkipHiddenFiles));
        OnPropertyChanged(nameof(MaxDepthLimit));
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(ShowFileExtensionStats));
        OnPropertyChanged(nameof(DefaultSortMode));
    }

    public AppSettings GetSavedSettings() => _current;

    private static AppSettings CopySettings(AppSettings s) => new()
    {
        SkipSystemFolders = s.SkipSystemFolders,
        SkipHiddenFiles = s.SkipHiddenFiles,
        MaxDepthLimit = s.MaxDepthLimit,
        DefaultSortMode = s.DefaultSortMode,
        Theme = s.Theme,
        ShowFileExtensionStats = s.ShowFileExtensionStats,
        LastScannedPath = s.LastScannedPath
    };
}
