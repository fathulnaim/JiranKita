using System.Globalization;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class AlertsPage : ContentPage
{
    // Firebase database service instance used to fetch approved neighborhood alerts
    private readonly FirebaseService fbService = new FirebaseService();

    // Local cache of all approved alerts fetched from Firebase, plus the active filter state
    private List<AlertModel> _allAlerts = new();
    private string _currentFilter = "All";

    // Default map center GPS coordinates (updated dynamically in SetMapCenterFromCommunity)
    private double _userLat = 3.0249;
    private double _userLng = 101.7012;

    // Computed property bound to the header label in AlertsPage.xaml
    public string CommunityTitleDisplay => $"Map: {AppSession.CurrentCommunityName}";

    public AlertsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    // Lifecycle method: runs every time the user navigates to this page; updates location & fetches fresh DB alerts
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(CommunityTitleDisplay));
        SetMapCenterFromCommunity();
        await LoadCommunityAlerts();
    }

    // Sets the map's center latitude and longitude based on the logged-in user's community code
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

    // Asynchronously fetches only JMB-approved and active alerts from Firebase, sorts newest-first, and updates UI
    private async Task LoadCommunityAlerts()
    {
        try
        {
            _allAlerts = await fbService.GetApprovedAlertsAsync(AppSession.CurrentCommunityCode);

            // Sort alerts chronologically (newest first) and generate a human-readable relative timestamp
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

    // Helper function: converts a UTC DateTime into a relative "time ago" string (e.g., "5 min ago")
    private static string BuildTimeAgo(DateTime createdAtUtc)
    {
        var diff = DateTime.UtcNow - createdAtUtc.ToUniversalTime();

        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hr ago";
        return $"{(int)diff.TotalDays} day(s) ago";
    }

    // Event handler: runs when a category filter pill button is tapped; updates filter & swaps button colors
    private void OnFilterAlertClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            string filterText = btn.Text;
            if (filterText.Contains("Road")) _currentFilter = "Road";
            else if (filterText.Contains("Hazards")) _currentFilter = "Hazards";
            else if (filterText.Contains("Security")) _currentFilter = "Security";
            else _currentFilter = "All";

            // Step 1: Reset all four filter pills back to their inactive white appearance
            ResetButton(PillAll);
            ResetButton(PillRoad);
            ResetButton(PillHazards);
            ResetButton(PillSecurity);

            // Step 2: Highlight the newly selected pill with Emerald Green background and white bold text
            btn.BackgroundColor = Color.FromArgb("#047857");
            btn.TextColor = Colors.White;
            btn.FontAttributes = FontAttributes.Bold;

            // Step 3: Re-filter the list and redraw map markers
            FilterAlertsAndRenderMap();
        }
    }

    // Helper function: restores a filter pill button to its unselected white/gray state
    private void ResetButton(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

    // Security helper: encodes HTML and escapes quotes/newlines so database text cannot break injected JavaScript
    private static string JsSafe(string? text) =>
        System.Net.WebUtility.HtmlEncode(text ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", " ")
            .Replace("\n", " ");

    // Core rendering function: filters alerts by category, updates CollectionView, and builds Leaflet OpenStreetMap HTML
    private void FilterAlertsAndRenderMap()
    {
        var filteredList = _allAlerts.AsEnumerable();
        if (_currentFilter != "All")
        {
            filteredList = filteredList.Where(a => a.Category.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase));
        }

        var activeAlerts = filteredList.ToList();
        AlertsCollectionView.ItemsSource = activeAlerts;

        // Use InvariantCulture so decimal coordinates always format with '.' instead of ','
        var inv = CultureInfo.InvariantCulture;
        string markersJs = string.Empty;

        foreach (var alert in activeAlerts)
        {
            // Skip alerts without valid GPS coordinates so they don't appear at (0, 0) in the ocean
            if (alert.Latitude == 0.0 && alert.Longitude == 0.0) continue;

            // Assign marker icon and hex color based on alert category
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

        // Generate complete HTML document embedding Leaflet.js + OpenStreetMap tiles + user geofence + alert markers
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

                // Blue marker representing the user's current community center
                L.circleMarker([{centerLat}, {centerLng}], {{
                    radius: 7,
                    fillColor: '#0284c7',
                    color: '#ffffff',
                    weight: 3,
                    opacity: 1,
                    fillOpacity: 1
                }}).addTo(map).bindPopup('<b>📍 You are here</b><br>{JsSafe(AppSession.CurrentUserUnit)}');

                // 500-meter green geofence radius circle around the community
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

    // Event handler: re-centers the map back to the community's default coordinates
    private void OnRecenterMapClicked(object sender, EventArgs e)
    {
        FilterAlertsAndRenderMap();
    }

    // Event handler: prompts confirmation and launches the native phone dialer for the guardhouse hotline
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

    // Event handler: navigates to CreateAlertPage so the resident can submit a new hazard report
    private async void OnReportHazardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.CreateAlertPage));
    }
}