namespace JiranKita.Views;

public partial class ChatPage : ContentPage
{
    public ChatPage()
    {
        InitializeComponent();
    }

    // ==========================================
    // BILA TEKAN ROBOT 🤖 -> PERGI KE JIRANBOT
    // ==========================================
    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }

    // Butang Send Mesej
    private void OnSendMessageClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TxtChatMessage.Text))
        {
            // Nanti di sini kita hantar mesej ke database/API chat
            TxtChatMessage.Text = string.Empty;
        }
    }

    // Indikator Fokus Hijau
    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
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