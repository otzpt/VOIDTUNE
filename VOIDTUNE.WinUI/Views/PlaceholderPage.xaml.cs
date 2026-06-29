using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class PlaceholderPage : Page
{
    public PlaceholderPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        TitleText.Text = (e.Parameter as string) switch
        {
            "services" => "Services — coming soon",
            "proc"     => "Processes — coming soon",
            "privacy"  => "Privacy — coming soon",
            _          => "Coming soon",
        };
    }
}
