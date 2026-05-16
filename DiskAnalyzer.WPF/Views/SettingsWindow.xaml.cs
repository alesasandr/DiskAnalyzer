using System.Windows;
using DiskAnalyzer.WPF.ViewModels;

namespace DiskAnalyzer.WPF.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
