namespace JiranKita;

public partial class AppShell : Shell
{
    // Constructor: initializes AppShell.xaml and registers all sub-page navigation routes outside the main TabBar
    public AppShell()
    {
        InitializeComponent();

        // Register all modal/detail/auth pages outside <TabBar> so Shell.Current.GoToAsync() can navigate to them
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
        Routing.RegisterRoute(nameof(Views.ChatRoomPage), typeof(Views.ChatRoomPage));
        Routing.RegisterRoute(nameof(Views.ApprovalsQueuePage), typeof(Views.ApprovalsQueuePage));
        Routing.RegisterRoute(nameof(Views.IssueNoticePage), typeof(Views.IssueNoticePage));
        Routing.RegisterRoute(nameof(Views.CreateAlertPage), typeof(Views.CreateAlertPage));
    }
}