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
        Routing.RegisterRoute(nameof(Views.JiranBotPage), typeof(Views.JiranBotPage));
        Routing.RegisterRoute(nameof(Views.CreatePostPage), typeof(Views.CreatePostPage));
        Routing.RegisterRoute(nameof(Views.PostDetailPage), typeof(Views.PostDetailPage));
        Routing.RegisterRoute(nameof(Views.ForgotPasswordPage), typeof(Views.ForgotPasswordPage));
        Routing.RegisterRoute(nameof(Views.RegisterCommunityPage), typeof(Views.RegisterCommunityPage));
    }
}