namespace JiranKita.Views;

public partial class JiranBotPage : ContentPage
{
    public JiranBotPage()
    {
        InitializeComponent();
    }

    // Automatically populates entry field when a quick inquiry chip is tapped
    private void OnQuickPromptTapped(object sender, TappedEventArgs e)
    {
        string prompt = e.Parameter?.ToString() ?? "";
        if (!string.IsNullOrEmpty(prompt))
        {
            TxtBotQuery.Text = $"Tell me about {prompt}";
            TxtBotQuery.Focus();
        }
    }

    // Focus listener: Highlights input field border with active emerald green stroke
    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
        }
    }

    // Unfocus listener: Resets input field border to neutral stone gray stroke
    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }
}