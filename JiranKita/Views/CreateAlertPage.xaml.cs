using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class CreateAlertPage : ContentPage
{
    // Firebase database service instance used to submit the resident's hazard report
    private readonly FirebaseService fbService = new FirebaseService();

    // Form state fields: tracks selected hazard category and captured GPS coordinates
    private string selectedCategory = "Hazards";
    private double _capturedLat = 0.0;
    private double _capturedLng = 0.0;
    private bool _hasRealGps = false;

    // Constructor: initializes XAML layout controls
    public CreateAlertPage()
    {
        InitializeComponent();
    }

    // Page startup lifecycle hook: sets safe community fallback coordinates first, then requests live device GPS
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SetCommunityDefaultCoordinates();
        await CaptureDeviceGpsAsync();
    }

    // Dynamically sets fallback coordinates based on the active community (avoids a single hardcoded location)
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

    // Requests runtime location permission and captures the device's real GPS coordinates for the GIS map pin
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
            // Fallback to active community coordinates already set in SetCommunityDefaultCoordinates()
        }

        LblGpsStatus.Text = _capturedLat != 0.0
            ? $"GPS Unavailable: Using {AppSession.CurrentCommunityName} area pin"
            : "GPS Unavailable: Location will be verified by JMB";
    }

    // Navigation handler: closes the form and returns to the previous page when Cancel is tapped
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // Event handler: extracts clean category text from the tapped pill button and highlights it in Crimson Red
    private void OnCategoryClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            // Strip emoji prefixes to store the clean category name (e.g., "Hazards", "Road", "Security")
            string cleanText = btn.Text.Replace("⚠️ ", "").Replace("🚧 ", "").Replace("🚨 ", "").Replace("💧 ", "").Replace("⚡ ", "").Replace("🛗 ", "");
            selectedCategory = cleanText;

            // Reset all 6 category buttons back to their unselected white appearance
            ResetButton(BtnCatHazards);
            ResetButton(BtnCatRoad);
            ResetButton(BtnCatSecurity);
            ResetButton(BtnCatWater);
            ResetButton(BtnCatElectricity);
            ResetButton(BtnCatFacility);

            // Highlight the newly selected category button in Crimson Red (#BE123C) with bold white text
            btn.BackgroundColor = Color.FromArgb("#BE123C");
            btn.TextColor = Colors.White;
            btn.FontAttributes = FontAttributes.Bold;
        }
    }

    // Helper function: restores a category button to its default unselected white/gray state
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

    // Validates user inputs, packages data into AlertModel, and sends it to Firebase for JMB moderation
    private async void OnSubmitAlertClicked(object sender, EventArgs e)
    {
        string title = TxtTitle.Text?.Trim() ?? "";
        string location = TxtLocation.Text?.Trim() ?? "";
        string advisory = TxtAdvisory.Text?.Trim() ?? "";

        // Validate required headline and location fields before building the database object
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(location))
        {
            await DisplayAlert("Information Required", "Please enter the incident title and location.", "OK");
            return;
        }

        // Provide a default safety message if the resident left the optional advisory box empty
        if (string.IsNullOrWhiteSpace(advisory))
        {
            advisory = "Please exercise caution when passing through this area.";
        }

        // Derive alert severity dynamically from the chosen category ("Security" -> HIGH, others -> MEDIUM)
        string derivedSeverity = selectedCategory == "Security" ? "HIGH" : "MEDIUM";

        // Package all form inputs, session user details, and GPS coordinates into a new AlertModel
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
            IsApproved = false, // Awaiting JMB approval in ApprovalsQueuePage before appearing on AlertsPage
            Severity = derivedSeverity,
            ReportedBy = $"{AppSession.CurrentUserName} ({AppSession.CurrentUserUnit})",
            Latitude = _capturedLat,
            Longitude = _capturedLng
        };

        try
        {
            // Push the new alert record to Firebase under the resident's community node
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

    // Focus visual feedback: turns the parent Border emerald green (#047857) when typing into an Entry
    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
        }
    }

    // Focus visual feedback: restores the parent Border to light gray (#D6D3D1) when leaving an Entry
    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }
}