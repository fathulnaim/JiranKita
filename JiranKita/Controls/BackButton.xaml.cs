namespace JiranKita.Controls;

public partial class BackButton : ContentView
{
    public BackButton()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}