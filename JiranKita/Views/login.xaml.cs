using JiranKita.Services;

namespace JiranKita.Views;

public partial class login : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private bool _isLoggingIn = false; // Prevents double-tap / concurrent login requests

    // Constructor: Initializes login page components
    public login()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Pre-fill saved credentials if Remember Me was enabled (only if fields are currently empty)
        if (Preferences.Default.Get("RememberMe", false) && string.IsNullOrWhiteSpace(TxtEmail.Text))
        {
            ChkRememberMe.IsChecked = true;
            TxtEmail.Text = Preferences.Default.Get("SavedEmail", string.Empty);
            TxtPassword.Text = Preferences.Default.Get("SavedPassword", string.Empty);
        }
    }

    // Validates credentials against Firebase, starts the user session, then routes the user
    private async void OnContinueClicked(object sender, EventArgs e)
    {
        if (_isLoggingIn) return;

        string email = TxtEmail.Text?.Trim() ?? "";
        string password = TxtPassword.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Missing Details", "Please enter your email and password.", "OK");
            return;
        }

        _isLoggingIn = true;

        try
        {
            var user = await _firebaseService.LoginUserAsync(email, password);

            if (user == null)
            {
                await DisplayAlert("Login Failed", "Incorrect email or password. Please try again.", "OK");
                return;
            }

            // Load the real user (full name, unit, role, community) into the app session
            AppSession.StartSession(user);

            // Persist or clear login credentials based on Remember Me state
            if (ChkRememberMe.IsChecked)
            {
                Preferences.Default.Set("RememberMe", true);
                Preferences.Default.Set("SavedEmail", email);
                Preferences.Default.Set("SavedPassword", password);
            }
            else
            {
                Preferences.Default.Remove("RememberMe");
                Preferences.Default.Remove("SavedEmail");
                Preferences.Default.Remove("SavedPassword");
            }

            // Community code is saved on the account, so only ask for it if never joined before
            if (AppSession.HasCommunity)
            {
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                await Shell.Current.GoToAsync("FindNeighbourhoodPage");
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", $"Unable to sign in: {error.Message}", "OK");
        }
        finally
        {
            _isLoggingIn = false;
        }
    }

    // Toggles the Remember Me checkbox when its text label is tapped
    private void OnRememberMeLabelTapped(object sender, TappedEventArgs e)
    {
        ChkRememberMe.IsChecked = !ChkRememberMe.IsChecked;
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

    // Toggles password visibility when the eye icon is tapped
    private void OnTogglePasswordTapped(object sender, TappedEventArgs e)
    {
        TxtPassword.IsPassword = !TxtPassword.IsPassword;
        LblTogglePassword.Text = TxtPassword.IsPassword ? "👁" : "🙈";
    }
}