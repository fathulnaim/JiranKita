using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class RegisterCommunityPage : ContentPage
{
    // Caches generated QR Code URL and 5-digit community invite code for downloading/sharing
    private string _currentQrUrl = string.Empty;
    private string _currentInviteCode = string.Empty;

    // Constructor: initializes XAML layout components
    public RegisterCommunityPage()
    {
        InitializeComponent();
    }

    // Media picker handler: opens device photo gallery to select JMB certificate image and displays its filename
    private async void OnUploadCertificateClicked(object sender, EventArgs e)
    {
        FileResult file = await MediaPicker.Default.PickPhotoAsync();
        if (file != null)
        {
            LblCertFileName.Text = $"Uploaded: {file.FileName}";
        }
    }

    // ==============================================================
    // 1. COMMUNITY REGISTRATION & FIREBASE PERSISTENCE
    // ==============================================================

    // Validates form inputs, generates a unique 5-character invite code + QR URL, and saves the community to Firebase
    private async void OnSubmitCommunityClicked(object sender, EventArgs e)
    {
        // 1. Validate required fields (Community Name and COB Registration Number)
        if (string.IsNullOrWhiteSpace(TxtCommunityName.Text) || string.IsNullOrWhiteSpace(TxtCobNumber.Text))
        {
            await DisplayAlert("Error", "Please fill in official community name and COB registration number.", "OK");
            return;
        }

        // 2. Generate a random 5-character alphanumeric invite code (e.g., KAJ82)
        string generatedInviteCode = GenerateRandomCode(5);

        // 3. Build the external QR Code API URL encoding the newly generated invite code
        _currentInviteCode = generatedInviteCode;
        _currentQrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={generatedInviteCode}";

        int.TryParse(TxtTotalUnits.Text?.Trim(), out int unitsCount);

        // 4. Package all form inputs, invite code, and QrCodeUrl into a new CommunityModel object
        var newCommunity = new CommunityModel
        {
            CommunityName = TxtCommunityName.Text?.Trim() ?? "",
            PropertyType = PickerPropertyType.SelectedItem?.ToString() ?? "High-Rise Strata",
            TotalUnits = unitsCount,
            CobRegNo = TxtCobNumber.Text?.Trim() ?? "",
            Address = TxtAddress.Text?.Trim() ?? "",
            RepName = TxtRepName.Text?.Trim() ?? "",
            RepPhone = TxtRepPhone.Text?.Trim() ?? "",
            CertificateFile = LblCertFileName.Text ?? "",
            InviteCode = generatedInviteCode,

            // Store the generated QR code image URL inside Firebase for future retrieval
            QrCodeUrl = _currentQrUrl,

            IsApproved = false,
            CreatedAt = DateTime.Now
        };

        try
        {
            // 5. Save the new community record to Firebase under the 'Communities' node
            var firebaseService = new FirebaseService();
            await firebaseService.RegisterCommunityAsync(newCommunity);

            // 6. Bind the generated QR image and invite code to the success popup modal
            ImgGeneratedQr.Source = _currentQrUrl;
            LblInviteCodeDisplay.Text = $"CODE: {generatedInviteCode}";

            // Reveal the success modal overlay on screen
            SuccessModal.IsVisible = true;
        }
        catch (Exception error)
        {
            await DisplayAlert("Firebase Error", $"Failed to register community: {error.Message}", "OK");
        }
    }

    // ==============================================================
    // 2. DOWNLOAD & SHARE QR CODE TO GALLERY / MESSAGING
    // ==============================================================

    // Downloads the generated QR image bytes to local cache and opens the native OS share/save sheet
    private async void OnDownloadQrClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentQrUrl)) return;

        try
        {
            // Fetch raw PNG image bytes from QR Server API
            using var httpClient = new HttpClient();
            byte[] imageBytes = await httpClient.GetByteArrayAsync(_currentQrUrl);

            // Write the PNG file locally into the app's cache directory
            string fileName = $"QR_JiranKita_{_currentInviteCode}.png";
            string localFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(localFilePath, imageBytes);

            // Prompt native system share sheet so JMB can save to gallery or print/send to residents
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"JiranKita Notice QR Code ({_currentInviteCode})",
                File = new ShareFile(localFilePath)
            });
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", $"Failed to download QR image: {error.Message}", "OK");
        }
    }

    // Modal dismiss handler: hides the success popup overlay and navigates back to the previous page
    private async void OnCloseSuccessModalClicked(object sender, EventArgs e)
    {
        SuccessModal.IsVisible = false;
        await Shell.Current.GoToAsync("..");
    }

    // Utility helper: generates a random uppercase alphanumeric string of the specified length
    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    // Focus state listener: highlights active Entry's parent Border with emerald green stroke (#047857)
    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
        }
    }

    // Focus state listener: resets Entry's parent Border back to default light gray stroke (#D6D3D1)
    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }
}