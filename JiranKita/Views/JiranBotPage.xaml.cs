namespace JiranKita.Views;

public partial class JiranBotPage : ContentPage
{
    public JiranBotPage()
    {
        InitializeComponent();
    }

    // Bila pengguna tekan mana-mana soalan lazim, auto isi kotak tanya
    private void OnQuickPromptTapped(object sender, TappedEventArgs e)
    {
        string prompt = e.Parameter?.ToString() ?? "";
        if (!string.IsNullOrEmpty(prompt))
        {
            TxtBotQuery.Text = $"Tell me about {prompt}";
            TxtBotQuery.Focus();
        }
    }

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