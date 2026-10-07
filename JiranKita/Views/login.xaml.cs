namespace JiranKita.Views;

public partial class login : ContentPage
{
    public login()
    {
        InitializeComponent();
    }
    private async void OnContinueClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("FindNeighbourhoodPage");
    }
}