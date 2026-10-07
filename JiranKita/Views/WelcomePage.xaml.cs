namespace JiranKita.Views;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    // Fungsi bila butang 'Continue to Sign In' ditekan
    private async void OnContinueClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("login");
    }
}