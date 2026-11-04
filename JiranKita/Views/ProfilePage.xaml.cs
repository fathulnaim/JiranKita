namespace JiranKita.Views;

public partial class ProfilePage : ContentPage
{
    // Constructor: Initializes ProfilePage layout components
    public ProfilePage()
    {
        InitializeComponent();
    }

    // Logout handler: Prompts confirmation alert and resets navigation route to Welcome Page
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Log Out", "Are you sure you want to log out of JiranKita?", "Yes, Log Out", "Cancel");
        if (confirm)
        {
            // Reset navigation stack and redirect to WelcomePage
            await Shell.Current.GoToAsync("//WelcomePage");
        }
    }

    // Navigation handler: Opens AboutPage view
    private async void OnAboutClicked(object sender, EventArgs e)
    {
        // Navigate to AboutPage route
        await Shell.Current.GoToAsync(nameof(Views.AboutPage));
    }
    private async void OnApprovalsQueuClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.ApprovalsQueuePage));
    }
    private async void OnIssueNoticeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.IssueNoticePage));
    }
}