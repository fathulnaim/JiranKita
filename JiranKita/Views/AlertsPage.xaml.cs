using System.Globalization;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class AlertsPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private List<AlertModel> _allAlerts = new();
    private string _currentFilter = "All";

    // Map center (set per community in SetMapCenterFromCommunity)
    private double _userLat = 3.0249;
    private double _userLng = 101.7012;

    public string CommunityTitleDisplay => $"GIS Map: {AppSession.CurrentCommunityName}";

    public AlertsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    // Lifecycle hook: runs every time the page appears; refreshes header and fetches fresh data
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(CommunityTitleDisplay));
        SetMapCenterFromCommunity();
        await LoadCommunityAlerts();
    }

    // Picks the map center based on the active community (same codes as CreateAlertPage)
    private void SetMapCenterFromCommunity()
    {
        string code = AppSession.CurrentCommunityCode?.Trim().ToUpper() ?? string.Empty;

        (_userLat, _userLng) = code switch
        {
            "KAJ82" => (2.9935, 101.7874), // Jade Hill Kajang
            "BGI99" => (2.9226, 101.7809), // Residensi Bangi Perdana
            _ => (3.0249, 101.7012)        // Default: Seri Kembangan
        };
    }

    // Pulls ONLY approved + active alerts from Firebase, then renders list and map
    private async Task LoadCommunityAlerts()
    {
        try
        {
            _allAlerts = await fbService.GetApprovedAlertsAsync(AppSession.CurrentCommunityCode);

            // Newest first, and compute a live "time ago" label from CreatedAt
            _allAlerts = _allAlerts.OrderByDescending(a => a.CreatedAt).ToList();
            foreach (var alert in _allAlerts)
            {
                alert.TimeDisplay = BuildTimeAgo(alert.CreatedAt);
            }

            FilterAlertsAndRenderMap();
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", $"Failed to load alerts: {error.Message}", "OK");
        }
    }

    private static string BuildTimeAgo(DateTime createdAtUtc)
    {
        var diff = DateTime.UtcNow - createdAtUtc.ToUniversalTime();

        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hr ago";
        return $"{(int)diff.TotalDays} day(s) ago";
    }

    // Filter pill tapped
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

    // Resets a filter pill to its unselected styling
    private void ResetButton(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

    // Makes any text safe to place inside a single-quoted JavaScript string with HTML content
    private static string JsSafe(string? text) =>
        System.Net.WebUtility.HtmlEncode(text ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", " ")
            .Replace("\n", " ");

    // Filters alerts, updates the list, builds Leaflet markers and renders the map
    private void FilterAlertsAndRenderMap()
    {
        var filteredList = _allAlerts.AsEnumerable();
        if (_currentFilter != "All")
        {
            filteredList = filteredList.Where(a => a.Category.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase));
        }

        var activeAlerts = filteredList.ToList();
        AlertsCollectionView.ItemsSource = activeAlerts;

        var inv = CultureInfo.InvariantCulture;
        string markersJs = string.Empty;

        foreach (var alert in activeAlerts)
        {
            // Skip alerts with no coordinates so they don't appear at 0,0 in the ocean
            if (alert.Latitude == 0.0 && alert.Longitude == 0.0) continue;

            bool isRoad = alert.Category.Equals("Road", StringComparison.OrdinalIgnoreCase);
            string markerIcon = isRoad ? "🚧" : "⚠️";
            string colorHex = isRoad ? "#F59E0B" : "#E11D48";

            markersJs += $@"
                L.circleMarker([{alert.Latitude.ToString(inv)}, {alert.Longitude.ToString(inv)}], {{
                    radius: 9,
                    fillColor: '{colorHex}',
                    color: '#ffffff',
                    weight: 2,
                    opacity: 1,
                    fillOpacity: 0.95
                }}).addTo(map).bindPopup('<b>{markerIcon} {JsSafe(alert.Title)}</b><br>{JsSafe(alert.Location)}');
            ";
        }

        string centerLat = _userLat.ToString(inv);
        string centerLng = _userLng.ToString(inv);

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
                var map = L.map('map', {{ zoomControl: false }}).setView([{centerLat}, {centerLng}], 15);

                L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
                    maxZoom: 19
                }}).addTo(map);

                L.circleMarker([{centerLat}, {centerLng}], {{
                    radius: 7,
                    fillColor: '#0284c7',
                    color: '#ffffff',
                    weight: 3,
                    opacity: 1,
                    fillOpacity: 1
                }}).addTo(map).bindPopup('<b>📍 You are here</b><br>{JsSafe(AppSession.CurrentUserUnit)}');

                L.circle([{centerLat}, {centerLng}], {{
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

    // Recenters the map (re-renders at the community default position)
    private void OnRecenterMapClicked(object sender, EventArgs e)
    {
        FilterAlertsAndRenderMap();
    }

    // SOS: confirmation dialog, then opens the phone dialer for the guardhouse hotline
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

    // Opens the report form (resident submits -> JMB approves -> shows here)
    private async void OnReportHazardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.CreateAlertPage));
    }
}