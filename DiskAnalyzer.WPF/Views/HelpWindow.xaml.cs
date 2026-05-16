using System.Windows;

namespace DiskAnalyzer.WPF.Views;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

public class ShortcutEntry
{
    public string Key { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
