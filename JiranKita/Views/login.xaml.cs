namespace JiranKita.Views;

public partial class login : ContentPage
{
    public login()
    {
        InitializeComponent();
    }

    // FUNGSI UNTUK PATAH BALIK KE SKRIN SEBELUMNYA
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        // ".." bermaksud 'pop' atau patah balik 1 langkah ke skrin sebelumnya (WelcomePage)
        await Shell.Current.GoToAsync("..");
    }
}