namespace JiranKita.Views;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
    }

    // Bila pengguna tekan 'Send Verification Code'
    private async void OnSendOtpClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtRecoveryInput.Text))
        {
            await DisplayAlert("Ralat", "Sila masukkan emel atau nombor telefon berdaftar anda.", "OK");
            return;
        }

        // Tunjuk notis & buka borang masukkan password baharu
        await DisplayAlert("Kod Dihantar", $"Kod pengesahan 6-digit telah dihantar ke: {TxtRecoveryInput.Text}", "Teruskan");
        ResetSection.IsVisible = true;
    }

    // Bila pengguna tekan 'Update Password'
    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNewPassword.Text) || TxtNewPassword.Text.Length < 6)
        {
            await DisplayAlert("Ralat", "Kata laluan baharu mestilah sekurang-kurangnya 6 aksara.", "OK");
            return;
        }

        if (TxtNewPassword.Text != TxtConfirmPassword.Text)
        {
            await DisplayAlert("Ralat", "Kata laluan baharu dan pengesahan tidak sepadan.", "OK");
            return;
        }

        await DisplayAlert("Berjaya!", "Kata laluan anda telah berjaya dikemas kini. Sila log masuk semula.", "Log Masuk");
        await Shell.Current.GoToAsync("..");
    }

    // Indikator Fokus Hijau
    private async void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            // 1. Tukar warna garisan jadi hijau
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;

            // 2. TUNGGU KEYBOARD NAIK, KEMUDIAN AUTO-TOLAK KOTAK TU KE ATAS!
            await Task.Delay(250); // tunggu keyboard telefon siap naik
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