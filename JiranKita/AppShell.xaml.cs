namespace JiranKita;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Hanya daftar skrin yang berada di luar TabBar:
        Routing.RegisterRoute("login", typeof(Views.login));
        Routing.RegisterRoute("FindNeighbourhoodPage", typeof(Views.FindNeighbourhoodPage));
        Routing.RegisterRoute("WelcomePage", typeof(Views.WelcomePage));
        Routing.RegisterRoute(nameof(Views.AboutPage), typeof(Views.AboutPage));
        Routing.RegisterRoute(nameof(Views.SignUpPage), typeof(Views.SignUpPage));
    }
}