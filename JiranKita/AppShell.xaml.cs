namespace JiranKita;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Daftar laluan ke skrin login
        Routing.RegisterRoute("login", typeof(Views.login));
    }
}