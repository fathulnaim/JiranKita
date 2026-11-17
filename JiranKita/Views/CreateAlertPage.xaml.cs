using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class CreateAlertPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private string selectedCategory = "Hazards";
    private double _capturedLat = 0.0;
    private double _capturedLng = 0.0;
    private bool _hasRealGps = false;

    public CreateAlertPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SetCommunityDefaultCoordinates();
        await CaptureDeviceGpsAsync();
    }

    // Dynamically sets fallback coordinates based on the active community (no single hardcoded location)
    private void SetCommunityDefaultCoordinates()
    {
        string code = AppSession.CurrentCommunityCode?.Trim().ToUpper() ?? string.Empty;

        (_capturedLat, _capturedLng) = code switch
        {
            "KAJ82" => (2.9935, 101.7874), // Jade Hill Kajang
            "BGI99" => (2.9226, 101.7809), // Residensi Bangi Perdana
            _ => (0.0, 0.0)
        };
    }

    // Captures the device's real GPS location to place a pin on the map
    private async Task CaptureDeviceGpsAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
                var location = await Geolocation.Default.GetLocationAsync(request);
                if (location != null)
                {
                    _capturedLat = location.Latitude;
                    _capturedLng = location.Longitude;
                    _hasRealGps = true;
                    LblGpsStatus.Text = $"GPS Detected: {_capturedLat:F4}, {_capturedLng:F4}";
                    return;
                }
            }
        }
        catch
        {
            // Fallback to active community coordinates set in SetCommunityDefaultCoordinates()
        }

        LblGpsStatus.Text = _capturedLat != 0.0
            ? $"GPS Unavailable: Using {AppSession.CurrentCommunityName} area pin"
            : "GPS Unavailable: Location will be verified by JMB";
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // Hazard Category Selection
    private void OnCategoryClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            // Ambil kategori bersih
            string cleanText = btn.Text.Replace("⚠️ ", "").Replace("🚧 ", "").Replace("🚨 ", "").Replace("💧 ", "").Replace("⚡ ", "").Replace("🛗 ", "");
            selectedCategory = cleanText;

            // Reset semua butang
            ResetButton(BtnCatHazards);
            ResetButton(BtnCatRoad);
            ResetButton(BtnCatSecurity);
            ResetButton(BtnCatWater);
            ResetButton(BtnCatElectricity);
            ResetButton(BtnCatFacility);

            // Butang aktif bertukar merah
            btn.BackgroundColor = Color.FromArgb("#BE123C");
            btn.TextColor = Colors.White;
            btn.FontAttributes = FontAttributes.Bold;
        }
    }

    private void ResetButton(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

    // ==========================================
    // SUBMIT REPORT TO FIREBASE (PENDING JMB)
    // ==========================================
    private async void OnSubmitAlertClicked(object sender, EventArgs e)
    {
        string title = TxtTitle.Text?.Trim() ?? "";
        string location = TxtLocation.Text?.Trim() ?? "";
        string advisory = TxtAdvisory.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(location))
        {
            await DisplayAlert("Information Required", "Please enter the incident title and location.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(advisory))
        {
            advisory = "Please exercise caution when passing through this area.";
        }

        // Derive severity dynamically from category if no separate severity picker exists in XAML
        string derivedSeverity = selectedCategory == "Security" ? "HIGH" : "MEDIUM";

        var newAlert = new AlertModel
        {
            Title = title.ToUpper(),
            Category = selectedCategory,
            Location = location,
            Description = advisory,
            Advisory = advisory,
            Distance = _hasRealGps ? "Live GPS Pin" : AppSession.CurrentCommunityName,
            TimeDisplay = "Just now",
            IsActive = true,
            IsApproved = false, // Awaiting JMB approval in ApprovalsQueuePage
            Severity = derivedSeverity,
            ReportedBy = $"{AppSession.CurrentUserName} ({AppSession.CurrentUserUnit})",
            Latitude = _capturedLat,
            Longitude = _capturedLng
        };

        try
        {
            await fbService.SubmitResidentAlertAsync(AppSession.CurrentCommunityCode, newAlert);

            await DisplayAlert(
                "Report Received 🛡️",
                "Thank you for your vigilance. This hazard report has been sent to the JMB management for safety review before being published to the community GIS map.",
                "Understood"
            );

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to submit report: {ex.Message}", "OK");
        }
    }

    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
        }
    }

    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }
}