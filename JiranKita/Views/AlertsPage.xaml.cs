using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class AlertsPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private List<AlertModel> _allAlerts = new();
    private string _currentFilter = "All";

    // GPS coordinates for community center (Seri Kembangan: 3.0249, 101.7012)
    private readonly double _userLat = 3.0249;
    private readonly double _userLng = 101.7012;

    public string CommunityTitleDisplay => $"GIS Map: {AppSession.CurrentCommunityName}";

    public AlertsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    // Lifecycle hook: Automatically called when page appears; refreshes header binding and fetches fresh data
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(CommunityTitleDisplay));
        await LoadCommunityAlerts();
    }

    // Async data fetcher: Pulls community alert records from Firebase service and triggers rendering
    private async Task LoadCommunityAlerts()
    {
        try
        {
            _allAlerts = await fbService.GetCommunityAlertsAsync(AppSession.CurrentCommunityCode);
            FilterAlertsAndRenderMap();
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", $"Failed to load alerts: {error.Message}", "OK");
        }
    }

    // Event handler: Triggered when user taps a filter pill button to switch categories
    private void OnFilterAlertClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            string filterText = btn.Text;
            if (filterText.Contains("Road")) _currentFilter = "Road";
            else if (filterText.Contains("Hazards")) _currentFilter = "Hazards";
            else if (filterText.Contains("Security")) _currentFilter = "Security";
            else _currentFilter = "All";

            ResetButton(PillAll);
            ResetButton(PillRoad);
            ResetButton(PillHazards);
            ResetButton(PillSecurity);

            btn.BackgroundColor = Color.FromArgb("#047857");
            btn.TextColor = Colors.White;
            btn.FontAttributes = FontAttributes.Bold;

            FilterAlertsAndRenderMap();
        }
    }

    // UI helper: Resets filter pill button back to unselected default styling
    private void ResetButton(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

    // Core engine: Filters alerts with LINQ, syncs CollectionView, builds dynamic Leaflet JS markers, and renders HTML in WebView
    private void FilterAlertsAndRenderMap()
    {
        var filteredList = _allAlerts.AsEnumerable();
        if (_currentFilter != "All")
        {
            filteredList = filteredList.Where(a => a.Category.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase));
        }

        var activeAlerts = filteredList.ToList();
        AlertsCollectionView.ItemsSource = activeAlerts;

        string markersJs = string.Empty;
        foreach (var alert in activeAlerts)
        {
            string markerIcon = alert.Category.ToLower() == "road" ? "🚧" : "⚠️";
            string colorHex = alert.Category.ToLower() == "road" ? "#F59E0B" : "#E11D48";

            markersJs += $@"
                L.circleMarker([{alert.Latitude}, {alert.Longitude}], {{
                    radius: 9,
                    fillColor: '{colorHex}',
                    color: '#ffffff',
                    weight: 2,
                    opacity: 1,
                    fillOpacity: 0.95
                }}).addTo(map).bindPopup('<b>{markerIcon} {alert.Title}</b><br>{alert.Location}');
            ";
        }

        string mapHtml = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no' />
            <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
            <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
            <style>
                body, html, #map {{ margin: 0; padding: 0; width: 100%; height: 100%; background: #e8ece9; }}
                .leaflet-popup-content {{ font-family: sans-serif; font-size: 11px; }}
            </style>
        </head>
        <body>
            <div id='map'></div>
            <script>
                var map = L.map('map', {{ zoomControl: false }}).setView([{_userLat}, {_userLng}], 15);
                
                L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
                    maxZoom: 19
                }}).addTo(map);

                L.circleMarker([{_userLat}, {_userLng}], {{
                    radius: 7,
                    fillColor: '#0284c7',
                    color: '#ffffff',
                    weight: 3,
                    opacity: 1,
                    fillOpacity: 1
                }}).addTo(map).bindPopup('<b>📍 You are here</b><br>{AppSession.CurrentUserUnit}');

                L.circle([{_userLat}, {_userLng}], {{
                    color: '#047857',
                    fillColor: '#10b981',
                    fillOpacity: 0.1,
                    radius: 500
                }}).addTo(map);

                {markersJs}
            </script>
        </body>
        </html>";

        MapWebView.Source = new HtmlWebViewSource { Html = mapHtml };
    }

    // Event handler: Recenters map view back to user default target GPS coordinates
    private void OnRecenterMapClicked(object sender, EventArgs e)
    {
        FilterAlertsAndRenderMap();
    }

    // Action handler: Displays confirmation dialog and triggers native PhoneDialer API to call guardhouse hotline
    private async void OnSosCallClicked(object sender, EventArgs e)
    {
        bool call = await DisplayAlert(
            "EMERGENCY CALL",
            $"Do you want to immediately call the {AppSession.CurrentCommunityName} Guardhouse?",
            "CALL NOW",
            "Cancel"
        );

        if (call)
        {
            try
            {
                string guardhouseNumber = "+60123456789";

                if (PhoneDialer.Default.IsSupported)
                {
                    PhoneDialer.Default.Open(guardhouseNumber);
                }
                else
                {
                    await DisplayAlert("Contact", $"Guardhouse Number: {guardhouseNumber}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to initiate call: {ex.Message}", "OK");
            }
        }
    }
}