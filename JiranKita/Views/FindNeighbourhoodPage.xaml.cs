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

        string qrText = firstResult.DisplayValue;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Hentikan kamera seketika supaya tidak berulang-ulang kali imbas
            QrScanner.CameraEnabled = false;

            // Bersihkan kod dan masukkan terus ke kotak 5 digit
            if (!string.IsNullOrEmpty(qrText))
            {
                string cleanCode = qrText.Trim();
                if (cleanCode.Length > 5)
                {
                    cleanCode = cleanCode.Substring(0, 5);
                }

                HiddenOtpEntry.Text = cleanCode.ToUpper();
            }

            await DisplayAlert("BERJAYA IMBAS!", $"Google ML Kit Berjaya Baca: {qrText}", "OK");
        });
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