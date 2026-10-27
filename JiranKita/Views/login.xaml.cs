namespace JiranKita.Views;

public partial class login : ContentPage
{
    // Constructor: Initializes login page components
    public login()
    {
        InitializeComponent();
    }

    // Handles sign in navigation to neighbourhood discovery page
    private async void OnContinueClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("FindNeighbourhoodPage");
    }

    // Navigates to the multi-step resident registration workflow
    private async void OnSignUpLinkClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.SignUpPage));
    }

    // Navigates to password recovery page
    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.ForgotPasswordPage));
    }
}