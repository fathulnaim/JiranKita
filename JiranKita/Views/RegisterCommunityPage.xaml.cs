using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class RegisterCommunityPage : ContentPage
{
    // Caches generated QR Code URL and 5-digit community invite code
    private string _currentQrUrl = string.Empty;
    private string _currentInviteCode = string.Empty;

    // Constructor: Initializes XAML layout components
    public RegisterCommunityPage()
    {
        InitializeComponent();
    }

    // Media picker handler: Selects JMB certificate image/document and updates label
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
    private async void OnSubmitCommunityClicked(object sender, EventArgs e)
    {
        // 1. Semak borang
        if (string.IsNullOrWhiteSpace(TxtCommunityName.Text) || string.IsNullOrWhiteSpace(TxtCobNumber.Text))
        {
            await DisplayAlert("Error", "Please fill in official community name and COB registration number.", "OK");
            return;
        }

        // 2. Jana kod 5-digit rawak (cth: KAJ82)
        string generatedInviteCode = GenerateRandomCode(5);

        // 3. JANA LINK URL QR CODE LEBIH AWAL DI SINI:
        _currentInviteCode = generatedInviteCode;
        _currentQrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={generatedInviteCode}";

        int.TryParse(TxtTotalUnits.Text?.Trim(), out int unitsCount);

        // 4. Masukkan QrCodeUrl ke dalam model yang akan dihantar ke DB
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

            // SIMPAN LINK QR INI KE DALAM FIREBASE:
            QrCodeUrl = _currentQrUrl,

            IsApproved = false,
            CreatedAt = DateTime.Now
        };

        try
        {
            // 5. Hantar ke Firebase (Dahan 'Communities')
            var firebaseService = new FirebaseService();
            await firebaseService.RegisterCommunityAsync(newCommunity);

            // 6. Paparkan pada imej pop-up skrin
            ImgGeneratedQr.Source = _currentQrUrl;
            LblInviteCodeDisplay.Text = $"CODE: {generatedInviteCode}";

            // Tunjukkan kad pop-up
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
    private async void OnDownloadQrClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentQrUrl)) return;

        try
        {
            // Fetch raw image stream bytes from QR Server API
            using var httpClient = new HttpClient();
            byte[] imageBytes = await httpClient.GetByteArrayAsync(_currentQrUrl);

            // Write image file locally to device cache directory
            string fileName = $"QR_JiranKita_{_currentInviteCode}.png";
            string localFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(localFilePath, imageBytes);

            // Prompt native system sheet to share or save photo to gallery
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

    // Modal dismiss handler: Hides success popup overlay and navigates back to previous screen
    private async void OnCloseSuccessModalClicked(object sender, EventArgs e)
    {
        SuccessModal.IsVisible = false;
        await Shell.Current.GoToAsync("..");
    }

    // Utility helper: Programmatically generates random alphanumeric code string
    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    // Focus state listener: Highlights active input border with emerald green stroke
    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
        }
    }

    // Focus state listener: Resets input border to default gray stroke
    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }
}