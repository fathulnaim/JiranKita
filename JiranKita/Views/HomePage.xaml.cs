using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class HomePage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();

    // Data Dinamik untuk Header
    public string CommunityNameDisplay => AppSession.CurrentCommunityName;
    public string UnitDisplay => AppSession.CurrentUserUnit;
    public string UserInitialsDisplay => AppSession.CurrentUserInitials;

    // Data Dinamik Amaran Kecemasan
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

        // Kemas kini paparan header mengikut sesi semasa
        OnPropertyChanged(nameof(CommunityNameDisplay));
        OnPropertyChanged(nameof(UnitDisplay));
        OnPropertyChanged(nameof(UserInitialsDisplay));

        await LoadHomeData();
    }

    private async Task LoadHomeData()
    {
        string code = AppSession.CurrentCommunityCode;

        try
        {
            // 1. Tarik amaran kecemasan terkini dari Firebase
            ActiveAlert = await fbService.GetUrgentAlertAsync(code);

            // 2. Tarik 2 pos paling terkini dari Firebase untuk 'Pulse'
            var recentPosts = await fbService.GetPulsePostsAsync(code, 2);
            PulseCollectionView.ItemsSource = recentPosts;
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat", $"Gagal memuatkan data Home: {error.Message}", "OK");
        }
    }

    // ==========================================
    // BUTANG QUICK ACTIONS
    // ==========================================
    private async void OnReportActionClicked(object sender, EventArgs e)
    {
        // Buka borang Create Post
        await Shell.Current.GoToAsync(nameof(Views.CreatePostPage));
    }

    private async void OnNoticesActionClicked(object sender, EventArgs e)
    {
        // Bawa terus ke tab Feed
        await Shell.Current.GoToAsync("//FeedPage");
    }

    private async void OnEventsActionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//FeedPage");
    }

    private async void OnJiranBotActionClicked(object sender, EventArgs e)
    {
        // Buka pembantu AI JiranBot
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }

    // ==========================================
    // BILA TEKAN POS PULSE -> BUKA POST DETAILS
    // ==========================================
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