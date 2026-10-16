namespace JiranKita.Views;

public partial class AlertsPage : ContentPage
{
    public AlertsPage()
    {
        InitializeComponent();
    }

    // ==========================================
    // FUNGSI PANGGILAN KECEMASAN SOS KE PONDOK PENGAWAL
    // ==========================================
    private async void OnSosCallClicked(object sender, EventArgs e)
    {
        bool call = await DisplayAlert(
            "PANGGILAN KECEMASAN",
            "Adakah anda ingin membuat panggilan kecemasan segera ke Pondok Pengawal (Guardhouse)?",
            "PANGGIL SEKARANG",
            "Batal"
        );

        if (call)
        {
            try
            {
                // Nombor rasmi pondok pengawal (nanti boleh diambil dari DB)
                string guardhouseNumber = "+60123456789";

                if (PhoneDialer.Default.IsSupported)
                {
                    PhoneDialer.Default.Open(guardhouseNumber);
                }
                else
                {
                    await DisplayAlert("Ralat", "Peranti ini tidak menyokong fungsi panggilan telefon.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ralat", $"Gagal memanggil: {ex.Message}", "OK");
            }
        }
    }
}