using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class DriversPage : Page
{
    private List<DriverItem> _all = new();
    private bool _loaded;

    public DriversPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (!_loaded) { _loaded = true; await LoadAsync(); }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async System.Threading.Tasks.Task LoadAsync()
    {
        BusyBar.Visibility = Visibility.Visible;
        CountLine.Text = "QUERYING DRIVERS…";
        _all = await DriverService.GetDriversAsync();
        BusyBar.Visibility = Visibility.Collapsed;

        var cats = _all.Select(d => d.Category).Distinct().OrderBy(c => c).ToList();
        CategoryBox.Items.Clear();
        CategoryBox.Items.Add("All categories");
        foreach (var c in cats) CategoryBox.Items.Add(c);
        CategoryBox.SelectedIndex = 0;
        Apply();
    }

    private void Category_Changed(object sender, SelectionChangedEventArgs e) => Apply();

    private void Apply()
    {
        if (_all.Count == 0) { DriverList.ItemsSource = null; CountLine.Text = "NO DRIVERS FOUND"; return; }
        string cat = CategoryBox.SelectedItem as string ?? "All categories";
        var items = cat == "All categories" ? _all : _all.Where(d => d.Category == cat).ToList();
        DriverList.ItemsSource = items;
        CountLine.Text = $"{items.Count} DRIVERS · {_all.Count} TOTAL";
    }
}
