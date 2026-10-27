namespace JiranKita.Views;

public partial class WelcomePage : ContentPage
{
    // Constructor: Initializes WelcomePage layout controls
    public WelcomePage()
    {
        InitializeComponent();
    }

    // Navigation handler: Triggered when 'Get Started !' button is tapped
    private async void OnContinueClicked(object sender, EventArgs e)
    {
        // Navigates user to the Login Page route via MAUI Shell
        await Shell.Current.GoToAsync("login");
    }
}