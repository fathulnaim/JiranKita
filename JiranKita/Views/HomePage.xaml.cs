using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class HomePage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();

    // Dynamic Binding Properties for Session Header
    public string CommunityNameDisplay => AppSession.CurrentCommunityName;
    public string UnitDisplay => AppSession.CurrentUserUnit;
    public string UserInitialsDisplay => AppSession.CurrentUserInitials;

    // Dynamic Urgent Alert Properties
    private AlertModel? _activeAlert;
    public AlertModel? ActiveAlert
    {
        get => _activeAlert;
        set { _activeAlert = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUrgentAlert)); }
    }
    public bool HasUrgentAlert => ActiveAlert != null && ActiveAlert.IsActive;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Refresh session header properties upon navigating to Page
        OnPropertyChanged(nameof(CommunityNameDisplay));
        OnPropertyChanged(nameof(UnitDisplay));
        OnPropertyChanged(nameof(UserInitialsDisplay));

        await LoadHomeData();
    }

    // Fetches live urgent emergency alert and latest posts from Firebase
    private async Task LoadHomeData()
    {
        string code = AppSession.CurrentCommunityCode;

        try
        {
            // 1. Fetch current urgent alert from Firebase
            ActiveAlert = await fbService.GetUrgentAlertAsync(code);

            // 2. Fetch top 2 recent posts for the 'Pulse' section
            var recentPosts = await fbService.GetPulsePostsAsync(code, 2);
            PulseCollectionView.ItemsSource = recentPosts;
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", $"Failed to load Home data: {error.Message}", "OK");
        }
    }

    // ==============================================================
    // QUICK ACTION EVENT HANDLERS
    // ==============================================================
    private async void OnReportActionClicked(object sender, EventArgs e)
    {
        // Route to Create Post page
        await Shell.Current.GoToAsync(nameof(Views.CreatePostPage));
    }

    private async void OnNoticesActionClicked(object sender, EventArgs e)
    {
        // Switch tab to Feed Page
        await Shell.Current.GoToAsync("//FeedPage");
    }

    private async void OnEventsActionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//FeedPage");
    }

    private async void OnJiranBotActionClicked(object sender, EventArgs e)
    {
        // Open JiranBot AI Assistant page
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }

    // Opens Post Details page when a pulse item is selected
    private async void OnPulsePostSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedPost = e.CurrentSelection.FirstOrDefault() as PostModel;
        if (selectedPost == null) return;

        PulseCollectionView.SelectedItem = null;

        var navData = new Dictionary<string, object>
        {
            { "SelectedPost", selectedPost }
        };

        await Shell.Current.GoToAsync(nameof(Views.PostDetailPage), navData);
    }
}