using BarcodeScanning;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class FindNeighbourhoodPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private bool isChecking = false; // Elak semakan bertindih serentak

    public FindNeighbourhoodPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Minta kebenaran kamera Google ML Kit
        await Methods.AskForRequiredPermissionAsync();
        QrScanner.CameraEnabled = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        QrScanner.CameraEnabled = false;
    }

    // ==========================================
    // 1. PENGESANAN KAMERA GOOGLE ML KIT
    // ==========================================
    private void OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
    {
        var firstResult = e.BarcodeResults.FirstOrDefault();
        if (firstResult == null) return;

        string qrText = firstResult.DisplayValue?.Trim() ?? "";

        // Tolak pautan web biasa (cth: https://)
        if (qrText.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;

        // Jika kod tepat 5 aksara, proses semakan
        if (qrText.Length == 5 && qrText.All(char.IsLetterOrDigit))
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                QrScanner.CameraEnabled = false;
                HiddenOtpEntry.Text = qrText.ToUpper();

                // Semak kod QR yang diimbas terus dengan Firebase!
                await CheckCodeWithFirebase(qrText.ToUpper());
            });
        }
    }

    // ==========================================
    // 2. KOTAK OTP 5-DIGIT & AUTO-SUBMIT
    // ==========================================
    private async void OnHiddenOtpTextChanged(object sender, TextChangedEventArgs e)
    {
        string text = e.NewTextValue?.ToUpper() ?? "";

        Lbl1.Text = text.Length > 0 ? text[0].ToString() : "";
        Lbl2.Text = text.Length > 1 ? text[1].ToString() : "";
        Lbl3.Text = text.Length > 2 ? text[2].ToString() : "";
        Lbl4.Text = text.Length > 3 ? text[3].ToString() : "";
        Lbl5.Text = text.Length > 4 ? text[4].ToString() : "";

        // Bila genap 5 digit ditaip: Terus semak dengan Firebase!
        if (text.Length == 5)
        {
            HiddenOtpEntry.Unfocus();
            await CheckCodeWithFirebase(text);
        }
    }

    // Butang manual "Join Now"
    private async void OnVerifyButtonClicked(object sender, EventArgs e)
    {
        string text = HiddenOtpEntry.Text?.Trim().ToUpper() ?? "";
        if (text.Length == 5)
        {
            await CheckCodeWithFirebase(text);
        }
        else
        {
            await DisplayAlert("Ralat", "Sila lengkapkan 5-digit kod jemputan.", "OK");
        }
    }

    // ==========================================
    // 3. FUNGSI SEMAK DENGAN DATABASE FIREBASE
    // ==========================================
    private async Task CheckCodeWithFirebase(string inviteCode)
    {
        if (isChecking) return;
        isChecking = true;

        try
        {
            // Tanya Firebase: Ada tak komuniti dengan kod ini?
            CommunityModel? matchedCommunity = await fbService.FindCommunityByInviteCodeAsync(inviteCode);

            if (matchedCommunity != null)
            {
                // ✅ KOD WUJUD & JUMPA DALAM FIREBASE!
                await DisplayAlert(
                    "Komuniti Ditemui! 🎉",
                    $"Selamat datang ke:\n\n{matchedCommunity.CommunityName}\n({matchedCommunity.PropertyType})\n\nMenyambungkan anda ke dashboard komuniti...",
                    "Masuk Sekarang"
                );

                // Masuk terus ke HomePage (TabBar rasmi akan aktif)
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                // ❌ KOD SALAH / TIADA DALAM DATABASE
                await DisplayAlert("Kod Tidak Sah", $"Tiada komuniti ditemui dengan kod '{inviteCode}'. Sila semak semula notis JMB anda.", "Cuba Lagi");

                // Kosongkan kotak supaya boleh taip semula
                HiddenOtpEntry.Text = string.Empty;
                QrScanner.CameraEnabled = true; // Hidupkan balik kamera
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat Firebase", $"Gagal menyemak kod: {error.Message}", "OK");
        }
        finally
        {
            isChecking = false;
        }
    }

    // Butang Flash
    private void OnTorchToggleClicked(object sender, EventArgs e)
    {
        QrScanner.TorchOn = !QrScanner.TorchOn;
    }

    // Butang Gallery
    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        FileResult photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo != null)
        {
            await DisplayAlert("Galeri", "Gambar telah dipilih.", "OK");
        }
    }

    // Pendaftaran Komuniti Baharu (JMB)
    private async void OnRegisterCommunityClicked(object sender, EventArgs e)
    {
        QrScanner.CameraEnabled = false; // Matikan kamera dulu supaya laju
        await Shell.Current.GoToAsync(nameof(Views.RegisterCommunityPage));
    }
}