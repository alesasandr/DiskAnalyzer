using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using DiskAnalyzer.Core.Interfaces;
using DiskAnalyzer.Core.Models;
using DiskAnalyzer.Core.Services;
using DiskAnalyzer.WPF.Commands;

namespace DiskAnalyzer.WPF.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IDriveProvider _driveProvider;
    private readonly ISettingsService _settingsService;
    private readonly IReportExporter _reportExporter;
    private AppSettings _settings;

    private CancellationTokenSource? _cts;
    private readonly Stopwatch _scanStopwatch = new();

    // --- Properties ---

    private ObservableCollection<DriveItem> _availableDrives = new();
    public ObservableCollection<DriveItem> AvailableDrives
    {
        get => _availableDrives;
        set => SetProperty(ref _availableDrives, value);
    }

    private DriveItem? _selectedDrive;
    public DriveItem? SelectedDrive
    {
        get => _selectedDrive;
        set
        {
            if (SetProperty(ref _selectedDrive, value) && value != null)
            {
                ScanPath = value.Name;
                OnPropertyChanged(nameof(DriveUsedPercent));
                OnPropertyChanged(nameof(DriveUsedFormatted));
                OnPropertyChanged(nameof(DriveFreeFormatted));
                OnPropertyChanged(nameof(DriveTotalFormatted));
            }
        }
    }

    private FolderNode? _rootNode;
    public FolderNode? RootNode
    {
        get => _rootNode;
        set => SetProperty(ref _rootNode, value);
    }

    private FileSystemNode? _selectedNode;
    public FileSystemNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            SetProperty(ref _selectedNode, value);
            OnPropertyChanged(nameof(SelectedNodeInfo));
        }
    }

    private ObservableCollection<FileSystemNode> _currentLevelItems = new();
    public ObservableCollection<FileSystemNode> CurrentLevelItems
    {
        get => _currentLevelItems;
        set => SetProperty(ref _currentLevelItems, value);
    }

    private ObservableCollection<BreadcrumbItem> _breadcrumbs = new();
    public ObservableCollection<BreadcrumbItem> Breadcrumbs
    {
        get => _breadcrumbs;
        set => SetProperty(ref _breadcrumbs, value);
    }

    private ObservableCollection<ExtensionStat> _extensionStats = new();
    public ObservableCollection<ExtensionStat> ExtensionStats
    {
        get => _extensionStats;
        set => SetProperty(ref _extensionStats, value);
    }

    private ObservableCollection<FileNode> _topFiles = new();
    public ObservableCollection<FileNode> TopFiles
    {
        get => _topFiles;
        set => SetProperty(ref _topFiles, value);
    }

    private SortMode _currentSortMode = SortMode.BySizeDescending;
    public SortMode CurrentSortMode
    {
        get => _currentSortMode;
        set { if (SetProperty(ref _currentSortMode, value)) ApplySort(); }
    }

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    private bool _isCancelling;
    public bool IsCancelling
    {
        get => _isCancelling;
        set => SetProperty(ref _isCancelling, value);
    }

    private ScanProgress _currentProgress = new();
    public ScanProgress CurrentProgress
    {
        get => _currentProgress;
        set => SetProperty(ref _currentProgress, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string _scanPath = string.Empty;
    public string ScanPath
    {
        get => _scanPath;
        set => SetProperty(ref _scanPath, value);
    }

    public double DriveUsedPercent => SelectedDrive?.UsedPercent ?? 0;
    public string DriveUsedFormatted => SelectedDrive?.FormattedUsed ?? "-";
    public string DriveFreeFormatted => SelectedDrive?.FormattedFree ?? "-";
    public string DriveTotalFormatted => SelectedDrive?.FormattedTotal ?? "-";

    public string SelectedNodeInfo => SelectedNode != null
        ? $"{SelectedNode.Name}  |  {SelectedNode.FormattedSize}  |  {SelectedNode.PercentOfRoot:F1}% of total"
        : string.Empty;

    // --- Commands ---

    public ICommand ScanCommand { get; }
    public ICommand CancelScanCommand { get; }
    public ICommand BrowseFolderCommand { get; }
    public ICommand NavigateIntoCommand { get; }
    public ICommand NavigateUpCommand { get; }
    public ICommand NavigateToBreadcrumbCommand { get; }
    public ICommand SortCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ExportTextCommand { get; }
    public ICommand OpenInExplorerCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand ShowHelpCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CopyPathCommand { get; }

    // Events for View dialogs
    public event Func<string?>? BrowseFolderRequested;
    public event Func<string, string?>? SaveFileRequested;
    public event Action? OpenSettingsRequested;
    public event Action? ShowHelpRequested;

    public MainViewModel(IDriveProvider driveProvider, ISettingsService settingsService, IReportExporter reportExporter)
    {
        _driveProvider = driveProvider;
        _settingsService = settingsService;
        _reportExporter = reportExporter;
        _settings = settingsService.Load();
        _currentSortMode = _settings.DefaultSortMode;

        ScanCommand = new RelayCommand(async _ => await StartScanAsync(), _ => !IsScanning);
        CancelScanCommand = new RelayCommand(_ => CancelScan(), _ => IsScanning && !IsCancelling);
        BrowseFolderCommand = new RelayCommand(() => BrowseFolder());
        NavigateIntoCommand = new RelayCommand(p => NavigateInto(p as FileSystemNode));
        NavigateUpCommand = new RelayCommand(_ => NavigateUp(), _ => Breadcrumbs.Count > 1);
        NavigateToBreadcrumbCommand = new RelayCommand(p => NavigateToBreadcrumb(p as BreadcrumbItem));
        SortCommand = new RelayCommand(p => { if (p is SortMode m) CurrentSortMode = m; });
        ExportCsvCommand = new RelayCommand(async _ => await ExportAsync("csv"), _ => RootNode != null && !IsScanning);
        ExportTextCommand = new RelayCommand(async _ => await ExportAsync("txt"), _ => RootNode != null && !IsScanning);
        OpenInExplorerCommand = new RelayCommand(_ => OpenInExplorer(), _ => SelectedNode != null);
        OpenSettingsCommand = new RelayCommand(() => OpenSettings());
        ShowHelpCommand = new RelayCommand(() => ShowHelpRequested?.Invoke());
        RefreshCommand = new RelayCommand(async _ => await StartScanAsync(), _ => !IsScanning && !string.IsNullOrEmpty(ScanPath));
        CopyPathCommand = new RelayCommand(_ => CopyPath(), _ => SelectedNode != null);

        LoadDrives();

        if (!string.IsNullOrEmpty(_settings.LastScannedPath))
            ScanPath = _settings.LastScannedPath;
    }

    private void LoadDrives()
    {
        AvailableDrives.Clear();
        foreach (var drive in _driveProvider.GetAvailableDrives())
            AvailableDrives.Add(drive);

        if (AvailableDrives.Count > 0)
            SelectedDrive = AvailableDrives[0];
    }

    private async Task StartScanAsync()
    {
        var path = ScanPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            StatusText = "Invalid path.";
            return;
        }

        _cts = new CancellationTokenSource();
        IsScanning = true;
        IsCancelling = false;
        RootNode = null;
        CurrentLevelItems.Clear();
        Breadcrumbs.Clear();
        ExtensionStats.Clear();
        TopFiles.Clear();
        _scanStopwatch.Restart();

        _settings = _settingsService.Load();
        _settings.LastScannedPath = path;
        _settingsService.Save(_settings);

        var progress = new Progress<ScanProgress>(p =>
        {
            CurrentProgress = p;
            StatusText = $"Scanning... {p.ScannedFiles:N0} files, {p.ScannedFolders:N0} folders";
        });

        try
        {
            var scanner = new DiskScanner(new RealFileSystemProvider(), _settings);
            var root = await scanner.ScanAsync(path, progress, _cts.Token);

            _scanStopwatch.Stop();
            RootNode = root;

            BuildBreadcrumbs(root);
            ShowFolderContents(root);

            var extStats = ExtensionAnalyzer.Analyze(root);
            ExtensionStats.Clear();
            foreach (var s in extStats) ExtensionStats.Add(s);

            var topFiles = ExtensionAnalyzer.GetTopLargestFiles(root, 50);
            TopFiles.Clear();
            foreach (var f in topFiles) TopFiles.Add(f);

            StatusText = $"Scanned: {root.FileCount:N0} files | {root.FolderCount:N0} folders | Total: {root.FormattedSize} | Time: {_scanStopwatch.Elapsed.TotalSeconds:F1}s";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            IsCancelling = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void CancelScan()
    {
        IsCancelling = true;
        _cts?.Cancel();
    }

    private void BrowseFolder()
    {
        var path = BrowseFolderRequested?.Invoke();
        if (path != null)
            ScanPath = path;
    }

    public void NavigateInto(FileSystemNode? node)
    {
        if (node is not FolderNode folder) return;

        Breadcrumbs.Add(new BreadcrumbItem(folder.Name, folder));
        ShowFolderContents(folder);
        SelectedNode = folder;
    }

    private void NavigateUp()
    {
        if (Breadcrumbs.Count <= 1) return;
        Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
        ShowFolderContents(Breadcrumbs[^1].Node);
    }

    private void NavigateToBreadcrumb(BreadcrumbItem? item)
    {
        if (item == null) return;
        var index = Breadcrumbs.IndexOf(item);
        if (index < 0) return;

        while (Breadcrumbs.Count > index + 1)
            Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);

        ShowFolderContents(item.Node);
    }

    private void BuildBreadcrumbs(FolderNode root)
    {
        Breadcrumbs.Clear();
        Breadcrumbs.Add(new BreadcrumbItem(root.Name, root));
    }

    private void ShowFolderContents(FolderNode folder)
    {
        var sorted = SortItems(folder.Children);
        CurrentLevelItems.Clear();
        foreach (var item in sorted)
            CurrentLevelItems.Add(item);
    }

    private IEnumerable<FileSystemNode> SortItems(IEnumerable<FileSystemNode> items) =>
        CurrentSortMode switch
        {
            SortMode.BySizeAscending => items.OrderBy(i => i.SizeBytes),
            SortMode.ByNameAscending => items.OrderBy(i => i.Name),
            SortMode.ByNameDescending => items.OrderByDescending(i => i.Name),
            SortMode.ByFileCountDescending => items.OrderByDescending(i => i is FolderNode f ? f.FileCount : 0),
            _ => items.OrderByDescending(i => i.SizeBytes)
        };

    private void ApplySort()
    {
        if (Breadcrumbs.Count == 0) return;
        ShowFolderContents(Breadcrumbs[^1].Node);
    }

    private async Task ExportAsync(string format)
    {
        if (RootNode == null) return;
        var ext = format == "csv" ? "csv" : "txt";
        var filter = format == "csv" ? "csv" : "txt";
        var path = SaveFileRequested?.Invoke(filter);
        if (path == null) return;

        try
        {
            if (format == "csv")
                await _reportExporter.ExportToCsvAsync(RootNode, path);
            else
                await _reportExporter.ExportToTextAsync(RootNode, path);

            StatusText = $"Exported to {path}";
        }
        catch (Exception ex)
        {
            StatusText = $"Export failed: {ex.Message}";
        }
    }

    private void OpenInExplorer()
    {
        if (SelectedNode == null) return;
        var path = SelectedNode.IsDirectory ? SelectedNode.FullPath : Path.GetDirectoryName(SelectedNode.FullPath);
        if (path != null)
            Process.Start("explorer.exe", path);
    }

    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
        _settings = _settingsService.Load();
        _currentSortMode = _settings.DefaultSortMode;
        OnPropertyChanged(nameof(CurrentSortMode));
    }

    private void CopyPath()
    {
        if (SelectedNode == null) return;
        System.Windows.Clipboard.SetText(SelectedNode.FullPath);
    }
}
