namespace JiranKita.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    // Fungsi Log Keluar (Logout)
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Log Keluar", "Adakah anda pasti mahu log keluar daripada JiranKita?", "Ya, Log Keluar", "Batal");
        if (confirm)
        {
            // Reset navigasi dan patah balik ke skrin Login
            await Shell.Current.GoToAsync("//WelcomePage");
        }
    }
}