using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class ScriptPage : Page
{
    public ScriptPage()
    {
        this.InitializeComponent();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ScriptBox.Text = "";
        OutputText.Text = "";
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        string body = ScriptBox.Text;
        if (string.IsNullOrWhiteSpace(body))
        {
            OutputText.Foreground = Hex("#F59E0B");
            OutputText.Text = "(nothing to run)";
            return;
        }

        bool powershell = ShellBox.SelectedIndex == 0;
        RunBtn.IsEnabled = false;
        BusyRing.IsActive = true;
        OutputText.Foreground = Hex("#88A0C8");
        OutputText.Text = "Running…";

        var r = await CommandRunner.RunScriptAsync(body, powershell);

        BusyRing.IsActive = false;
        RunBtn.IsEnabled = true;
        OutputText.Foreground = Hex(r.Ok ? "#C8E6C9" : "#EF9A9A");
        OutputText.Text = string.IsNullOrWhiteSpace(r.Output)
            ? (r.Ok ? "(completed, no output)" : "(failed, no output)")
            : r.Output;
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush Hex(string hex)
    {
        byte r = System.Convert.ToByte(hex.Substring(1, 2), 16);
        byte g = System.Convert.ToByte(hex.Substring(3, 2), 16);
        byte b = System.Convert.ToByte(hex.Substring(5, 2), 16);
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
    }
}
