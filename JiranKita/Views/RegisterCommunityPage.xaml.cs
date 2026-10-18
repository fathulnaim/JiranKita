using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class RegisterCommunityPage : ContentPage
{
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

            // 3. JANA GAMBAR QR CODE & PAPARKAN POP-UP DI SKRIN!
            ImgGeneratedQr.Source = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={generatedInviteCode}";
            LblInviteCodeDisplay.Text = $"KOD: {generatedInviteCode}";

            // Tunjukkan kad pop-up
            SuccessModal.IsVisible = true;
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat Firebase", $"Gagal mendaftar: {error.Message}", "OK");
        }
    }

    // Butang tutup pop-up selepas tengok QR
    private async void OnCloseSuccessModalClicked(object sender, EventArgs e)
    {
        SuccessModal.IsVisible = false;
        await Shell.Current.GoToAsync("..");
    }

    // Fungsi kecil untuk jana 5 huruf/nombor rawak (seperti KAJ82)
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