using System.Windows;
using DiskAnalyzer.Core.Services;
using DiskAnalyzer.WPF.Views;
using DiskAnalyzer.WPF.ViewModels;

namespace DiskAnalyzer.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new JsonSettingsService();
        var settings = settingsService.Load();

        // Apply theme
        ApplyTheme(settings.Theme);

        var driveProvider = new DriveProvider();
        var exporter = new CsvReportExporter();
        var vm = new MainViewModel(driveProvider, settingsService, exporter);

        var window = new MainWindow(vm);
        window.Show();
    }

    public void ApplyTheme(string theme)
    {
        var dict = new ResourceDictionary();
        dict.Source = theme == "Light"
            ? new Uri("Themes/LightTheme.xaml", UriKind.Relative)
            : new Uri("Themes/DarkTheme.xaml", UriKind.Relative);

        // Replace first merged dictionary (the theme)
        Resources.MergedDictionaries[0] = dict;
    }
}
