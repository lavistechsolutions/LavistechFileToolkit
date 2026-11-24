using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace LavistechFileToolkit;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version?.ToString(3) ?? "1.0.0";

        FeedbackLink.MouseLeftButtonUp += (_, _) => Open("https://lavistech.com/contact");
        LicenseLink.MouseLeftButtonUp += (_, _) => OpenDocument("LICENSE");
        EulaLink.MouseLeftButtonUp += (_, _) => OpenDocument("EULA.txt");
        NoticesLink.MouseLeftButtonUp += (_, _) => OpenDocument("THIRD-PARTY-NOTICES.txt");
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };
    }

    void OpenDocument(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, fileName);
        if (!File.Exists(path))
        {
            MessageBox.Show(this, $"{fileName} is not next to the application.",
                "Lavistech File Toolkit", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Open(path);
    }

    static void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Lavistech File Toolkit", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
