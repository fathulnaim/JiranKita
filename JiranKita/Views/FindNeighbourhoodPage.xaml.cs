using BarcodeScanning;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class FindNeighbourhoodPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private bool _isChecking = false; // Prevents concurrent validation requests

    // Constructor: Initializes layout controls
    public FindNeighbourhoodPage()
    {
        InitializeComponent();
    }

    // Lifecycle hook: Requests camera permissions and enables scanner on page focus
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Methods.AskForRequiredPermissionAsync();
        QrScanner.CameraEnabled = true;
    }

    // Lifecycle hook: Disables camera when navigating away to release hardware resources
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        QrScanner.CameraEnabled = false;
    }

    // Camera detection callback: Handles Google ML Kit barcode scanner results
    private void OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
    {
        var firstResult = e.BarcodeResults.FirstOrDefault();
        if (firstResult == null) return;

        string qrText = firstResult.DisplayValue?.Trim() ?? "";

        // Reject standard HTTP web URLs
        if (qrText.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;

        // Process valid 5-character alphanumeric community codes
        if (qrText.Length == 5 && qrText.All(char.IsLetterOrDigit))
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                QrScanner.CameraEnabled = false;
                HiddenOtpEntry.Text = qrText.ToUpper();

                // Validate scanned code against Firebase database records
                await CheckCodeWithFirebase(qrText.ToUpper());
            });
        }
    }

    // Input handler: Formats typed OTP characters across 5 visual border boxes and auto-submits upon completion
    private async void OnHiddenOtpTextChanged(object sender, TextChangedEventArgs e)
    {
        string text = e.NewTextValue?.ToUpper() ?? "";

        Lbl1.Text = text.Length > 0 ? text[0].ToString() : "";
        Lbl2.Text = text.Length > 1 ? text[1].ToString() : "";
        Lbl3.Text = text.Length > 2 ? text[2].ToString() : "";
        Lbl4.Text = text.Length > 3 ? text[3].ToString() : "";
        Lbl5.Text = text.Length > 4 ? text[4].ToString() : "";

        // Auto-submit validation when 5 characters are entered
        if (text.Length == 5)
        {
            HiddenOtpEntry.Unfocus();
            await CheckCodeWithFirebase(text);
        }
    }

    // Action handler: Manual validation trigger for "Join Now" button
    private async void OnVerifyButtonClicked(object sender, EventArgs e)
    {
        string text = HiddenOtpEntry.Text?.Trim().ToUpper() ?? "";
        if (text.Length == 5)
        {
            await CheckCodeWithFirebase(text);
        }
        else
        {
            await DisplayAlert("Error", "Please enter a complete 5-digit invite code.", "OK");
        }
    }

    // Firebase verification logic: Checks database for matching community invite codes
    private async Task CheckCodeWithFirebase(string inviteCode)
    {
        if (_isChecking) return;
        _isChecking = true;

        try
        {
            CommunityModel? matchedCommunity = await _firebaseService.FindCommunityByInviteCodeAsync(inviteCode);

            if (matchedCommunity != null)
            {
                // Active match found
                AppSession.CurrentCommunityCode = matchedCommunity.InviteCode;
                AppSession.CurrentCommunityName = matchedCommunity.CommunityName;

                await DisplayAlert(
                    "Community Found! 🎉",
                    $"Welcome to:\n\n{matchedCommunity.CommunityName}\n({matchedCommunity.PropertyType})\n\nConnecting you to your community dashboard...",
                    "Enter Now"
                );

                // Navigate directly to main AppShell HomePage root
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                // Invalid code fallback
                await DisplayAlert("Invalid Code", $"No community was found matching code '{inviteCode}'. Please verify your JMB notice board details.", "Try Again");

                // Clear input and resume scanner hardware
                HiddenOtpEntry.Text = string.Empty;
                QrScanner.CameraEnabled = true;
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Firebase Error", $"Failed to verify invite code: {error.Message}", "OK");
        }
        finally
        {
            _isChecking = false;
        }
    }

    // Action handler: Toggles camera flash hardware
    private void OnTorchToggleClicked(object sender, EventArgs e)
    {
        QrScanner.TorchOn = !QrScanner.TorchOn;
    }

    // Action handler: Picks photo from device gallery
    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        FileResult photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo != null)
        {
            await DisplayAlert("Gallery", "Photo selected.", "OK");
        }
    }

    // Action handler: Navigates JMB administrators to new community registration page
    private async void OnRegisterCommunityClicked(object sender, EventArgs e)
    {
        QrScanner.CameraEnabled = false;
        await Shell.Current.GoToAsync(nameof(Views.RegisterCommunityPage));
    }
}