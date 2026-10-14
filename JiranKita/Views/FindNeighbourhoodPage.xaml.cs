using BarcodeScanning;

namespace JiranKita.Views;

public partial class FindNeighbourhoodPage : ContentPage
{
    public FindNeighbourhoodPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Minta kebenaran kamera rasmi Google ML Kit automatik
        await Methods.AskForRequiredPermissionAsync();
        QrScanner.CameraEnabled = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        QrScanner.CameraEnabled = false;
    }

    // FUNGSI PENGESANAN GOOGLE ML KIT (Sangat laju & stabil)
    private void OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
    {
        var firstResult = e.BarcodeResults.FirstOrDefault();
        if (firstResult == null) return;

        string qrText = firstResult.DisplayValue?.Trim() ?? "";

        // 1. TOLAK jika ia adalah link website (https / http)
        if (qrText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            qrText.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Abaikan link web, biarkan kamera terus mencari QR yang betul
            return;
        }

        // 2. PENAPIS KHUSUS: Pastikan kod tepat 5 huruf/nombor sahaja (Contoh: KAJ82)
        if (qrText.Length == 5 && qrText.All(char.IsLetterOrDigit))
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Matikan kamera sekejap supaya tak scan berulang
                QrScanner.CameraEnabled = false;

                // Masukkan kod ke kotak invite code
                HiddenOtpEntry.Text = qrText.ToUpper();

                await DisplayAlert("BERJAYA!", $"Kod Komuniti Dikesan: {qrText.ToUpper()}", "OK");
                await Shell.Current.GoToAsync(nameof(Views.HomePage));
            });
        }
        else
        {
            // Jika orang imbas QR lain yang salah (contohnya QR menu kedai / MySejahtera)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Pause sekejap supaya amaran tak keluar bertubi-tubi
                QrScanner.CameraEnabled = false;

                await DisplayAlert("QR Tidak Sah", "Sila imbas QR Notis JiranKita yang sah sahaja.", "Cuba Lagi");

                // Hidupkan semula kamera untuk imbas semula
                QrScanner.CameraEnabled = true;
            });
        }
    }

    // Butang Flash Lampu Picit
    private void OnTorchToggleClicked(object sender, EventArgs e)
    {
        QrScanner.TorchOn = !QrScanner.TorchOn;
    }

    // Kotak OTP 5-digit auto lompat & auto-submit
    private async void OnHiddenOtpTextChanged(object sender, TextChangedEventArgs e)
    {
        string text = e.NewTextValue?.ToUpper() ?? "";

        Lbl1.Text = text.Length > 0 ? text[0].ToString() : "";
        Lbl2.Text = text.Length > 1 ? text[1].ToString() : "";
        Lbl3.Text = text.Length > 2 ? text[2].ToString() : "";
        Lbl4.Text = text.Length > 3 ? text[3].ToString() : "";
        Lbl5.Text = text.Length > 4 ? text[4].ToString() : "";

        if (text.Length == 5)
        {
            HiddenOtpEntry.Unfocus();
            await DisplayAlert("Pengesahan Kod", $"Kod '{text}' berjaya disahkan!", "OK");
            await Shell.Current.GoToAsync(nameof(Views.HomePage));
        }
    }

    // Butang Manual Join Now
    private async void OnVerifyButtonClicked(object sender, EventArgs e)
    {
        string text = HiddenOtpEntry.Text?.ToUpper() ?? "";
        if (text.Length == 5)
        {
            await DisplayAlert("Pengesahan Kod", $"Kod '{text}' berjaya disahkan!", "OK");
        }
        else
        {
            await DisplayAlert("Ralat", "Sila lengkapkan 5-digit kod jemputan.", "OK");
        }
    }

    // Butang Gallery
    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        FileResult photo = await MediaPicker.Default.PickPhotoAsync();

        if (photo != null)
        {
            string localFilePath = photo.FullPath;
            await DisplayAlert("Berjaya", "Gambar dipilih dari galeri!", "OK");
        }
    }
}