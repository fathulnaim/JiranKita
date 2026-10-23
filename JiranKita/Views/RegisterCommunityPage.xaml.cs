using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class RegisterCommunityPage : ContentPage
{
    // Simpan pautan QR dan kod jemputan semasa
    private string _currentQrUrl = string.Empty;
    private string _currentInviteCode = string.Empty;

    public RegisterCommunityPage()
    {
        InitializeComponent();
    }

    private async void OnUploadCertificateClicked(object sender, EventArgs e)
    {
        FileResult file = await MediaPicker.Default.PickPhotoAsync();
        if (file != null)
        {
            LblCertFileName.Text = $"Uploaded: {file.FileName}";
        }
    }

    // ==========================================
    // 1. HANTAR PENDAFTARAN KOMUNITI KE FIREBASE
    // ==========================================
    private async void OnSubmitCommunityClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCommunityName.Text) || string.IsNullOrWhiteSpace(TxtCobNumber.Text))
        {
            await DisplayAlert("Ralat", "Sila lengkapkan nama komuniti dan nombor pendaftaran COB.", "OK");
            return;
        }

        // 1. Jana kod 5-digit rawak (cth: KAJ82)
        string generatedInviteCode = GenerateRandomCode(5);

        int.TryParse(TxtTotalUnits.Text?.Trim(), out int unitsCount);

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
            IsApproved = false
        };

        try
        {
            // 2. Simpan ke Firebase
            var fbService = new FirebaseService();
            await fbService.RegisterCommunityAsync(newCommunity);

            // 3. Simpan maklumat QR dan paparkan pop-up
            _currentInviteCode = generatedInviteCode;
            _currentQrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={generatedInviteCode}";

            ImgGeneratedQr.Source = _currentQrUrl;
            LblInviteCodeDisplay.Text = $"KOD: {generatedInviteCode}";

            // Tunjukkan kad pop-up kejayaan
            SuccessModal.IsVisible = true;
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat Firebase", $"Gagal mendaftar: {error.Message}", "OK");
        }
    }

    // ==========================================
    // 2. MUAT TURUN / SIMPAN QR KE GALERI TELEFON
    // ==========================================
    private async void OnDownloadQrClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentQrUrl)) return;

        try
        {
            // A. Muat turun gambar QR terus ke memori
            using var httpClient = new HttpClient();
            byte[] imageBytes = await httpClient.GetByteArrayAsync(_currentQrUrl);

            // B. Simpan fail gambar ke storan sementara telefon
            string fileName = $"QR_JiranKita_{_currentInviteCode}.png";
            string localFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(localFilePath, imageBytes);

            // C. Buka menu telefon (Pengguna boleh tekan 'Save Image' untuk simpan ke galeri atau kongsi ke WhatsApp)
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"QR Code Notis JiranKita ({_currentInviteCode})",
                File = new ShareFile(localFilePath)
            });
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat", $"Gagal memuat turun: {error.Message}", "OK");
        }
    }

    // Butang tutup pop-up selepas tengok / muat turun QR
    private async void OnCloseSuccessModalClicked(object sender, EventArgs e)
    {
        SuccessModal.IsVisible = false;
        await Shell.Current.GoToAsync("..");
    }

    // Fungsi kecil untuk jana 5 huruf/nombor rawak
    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    // Indikator Fokus Hijau
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