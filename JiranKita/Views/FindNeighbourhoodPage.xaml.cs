using ZXing.Net.Maui;

namespace JiranKita.Views;

public partial class FindNeighbourhoodPage : ContentPage
{
    public FindNeighbourhoodPage()
    {
        InitializeComponent();
       
    }

    private async void OnHiddenOtpTextChanged(object sender, TextChangedEventArgs e)
    {
        string text = e.NewTextValue?.ToUpper() ?? "";

        // Auto isi setiap kotak mengikut apa yang ditaip / dipadam
        Lbl1.Text = text.Length > 0 ? text[0].ToString() : "";
        Lbl2.Text = text.Length > 1 ? text[1].ToString() : "";
        Lbl3.Text = text.Length > 2 ? text[2].ToString() : "";
        Lbl4.Text = text.Length > 3 ? text[3].ToString() : "";
        Lbl5.Text = text.Length > 4 ? text[4].ToString() : "";

        // BILA CUKUP 5 DIGIT: TERUS AUTO-SUBMIT!
        if (text.Length == 5)
        {
            HiddenOtpEntry.Unfocus(); // Tutup keyboard
            await DisplayAlert("Pengesahan Kod", $"Kod '{text}' berjaya disahkan!", "OK");
        }
    }

    // Butang hijau manual di bawah
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

    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        // Buka galeri/album gambar telefon
        FileResult photo = await MediaPicker.Default.PickPhotoAsync();

        if (photo != null)
        {
            string localFilePath = photo.FullPath;
            await DisplayAlert("Berjaya", "Gambar dipilih dari galeri!", "OK");
        }
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var firstResult = e.Results.FirstOrDefault();
        if (firstResult == null) return;

        // Ambil teks yang ada dalam QR Code tersebut
        string qrText = firstResult.Value;

        // Kamera berjalan di 'background thread', jadi kena hantar ke 'MainThread' untuk ubah UI
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Matikan kamera sekejap supaya tak scan berkali-kali
            QrScanner.IsDetecting = false;

            // Masukkan kod QR tadi terus ke dalam kotak input & auto-submit!
            HiddenOtpEntry.Text = qrText;

            await DisplayAlert("QR Dikesan!", $"Berjaya mengimbas kod: {qrText}", "OK");
        });
    }

    // Fungsi On/Off Lampu Flash
    private void OnTorchToggleClicked(object sender, EventArgs e)
    {
        QrScanner.IsTorchOn = !QrScanner.IsTorchOn;
    }


}