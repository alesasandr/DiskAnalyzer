using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiskAnalyzer.Core.Models;
using DiskAnalyzer.Core.Services;
using DiskAnalyzer.WPF.ViewModels;
using Microsoft.Win32;

namespace DiskAnalyzer.WPF.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.BrowseFolderRequested += BrowseFolder;
        vm.SaveFileRequested += SaveFile;
        vm.OpenSettingsRequested += OpenSettings;
        vm.ShowHelpRequested += ShowHelp;

        // Build TreeView when root changes
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.RootNode) && vm.RootNode != null)
            {
                FolderTree.Items.Clear();
                FolderTree.Items.Add(vm.RootNode);
            }
        };
    }

    private string? BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder to scan"
        };
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }

    private string? SaveFile(string format)
    {
        var dialog = new SaveFileDialog
        {
            Filter = format == "csv"
                ? "CSV files (*.csv)|*.csv"
                : "Text files (*.txt)|*.txt",
            DefaultExt = format
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private void OpenSettings()
    {
        var settingsService = new DiskAnalyzer.Core.Services.JsonSettingsService();
        var settingsVm = new SettingsViewModel(settingsService);
        var win = new SettingsWindow(settingsVm);
        settingsVm.RequestClose += (_, _) => win.Close();
        win.Owner = this;
        win.ShowDialog();

        // Reload theme if changed
        if (settingsVm.DialogResult == true)
        {
            ((App)Application.Current).ApplyTheme(settingsVm.Theme);
        }
    }

    private void ShowHelp()
    {
        var win = new HelpWindow { Owner = this };
        win.ShowDialog();
    }

    private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FolderNode folder)
        {
            // Navigate breadcrumbs to this folder
            _vm.SelectedNode = folder;
        }
    }

    private void ContentGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.SelectedNode is FolderNode folder)
            _vm.NavigateInto(folder);
    }
}
