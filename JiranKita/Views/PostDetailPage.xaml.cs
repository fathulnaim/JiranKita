namespace JiranKita.Views;

public partial class PostDetailPage : ContentPage
{
    public PostDetailPage()
    {
        InitializeComponent();
    }

    private async void OnContactOwnerClicked(object sender, EventArgs e)
    {
        // Contoh panggilan terus ke nombor pemilik
        if (PhoneDialer.Default.IsSupported)
        {
            PhoneDialer.Default.Open("+60123456789");
        }
        else
        {
            await DisplayAlert("Hubungi", "Panggilan ke pemilik: +60 12-345 6789", "OK");
        }
    }
}