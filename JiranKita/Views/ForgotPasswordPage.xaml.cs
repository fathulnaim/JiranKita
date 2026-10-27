namespace JiranKita.Views;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
    }

    // Triggered when user clicks 'Send Verification Code'
    private async void OnSendOtpClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtRecoveryInput.Text))
        {
            await DisplayAlert("Error", "Please enter your registered email address or phone number.", "OK");
            return;
        }

        // Prompt alert notice and reveal password reset section
        await DisplayAlert("Code Sent", $"A 6-digit verification code has been sent to: {TxtRecoveryInput.Text}", "Proceed");
        ResetSection.IsVisible = true;
    }

    // Triggered when user clicks 'Update Password & Sign In'
    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNewPassword.Text) || TxtNewPassword.Text.Length < 6)
        {
            await DisplayAlert("Error", "New password must be at least 6 characters.", "OK");
            return;
        }

        if (TxtNewPassword.Text != TxtConfirmPassword.Text)
        {
            await DisplayAlert("Error", "New password and confirmation do not match.", "OK");
            return;
        }

        await DisplayAlert("Success!", "Your password has been successfully updated. Please sign in again.", "Sign In");
        await Shell.Current.GoToAsync("..");
    }

    // Green focus border indicator with auto-scroll accessibility adjustments for mobile keyboard
    private async void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            // 1. Highlight border in green
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;

            // 2. Delay slightly for soft keyboard popup, then auto-scroll target into view
            await Task.Delay(250);
            await ResetScrollView.ScrollToAsync(border, ScrollToPosition.Center, true);
        }
    }

    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }
}