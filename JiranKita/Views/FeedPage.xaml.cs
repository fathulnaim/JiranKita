using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class FeedPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private List<PostModel> _allMasterPosts = new();
    private string _currentFilterCategory = "All";

    // Constructor: Initializes XAML layout elements
    public FeedPage()
    {
        InitializeComponent();
    }

    // Lifecycle hook: Refreshes community feed posts whenever page comes into focus
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFeedPostsFromFirebase();
    }

    // Async data fetcher: Pulls feed posts matching active session community code
    private async Task LoadFeedPostsFromFirebase()
    {
        try
        {
            _allMasterPosts = await _firebaseService.GetAllPostsAsync(AppSession.CurrentCommunityCode);
            ApplyFilterAndSearch();
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", $"Failed to load posts: {error.Message}", "OK");
        }
    }

    // Event handler: Triggered on search text changes to apply keyword filters
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilterAndSearch();
    }

    // Event handler: Manages category filter pill selection and updates active button styling
    private void OnFilterPillClicked(object sender, EventArgs e)
    {
        if (sender is Button clickedBtn)
        {
            string cat = clickedBtn.Text.Replace("&amp;", "&");
            if (cat.Contains("Road")) cat = "Road & Maintenance";
            _currentFilterCategory = cat;

            // Reset all filter pills back to default white state
            ResetPill(PillAll);
            ResetPill(PillLost);
            ResetPill(PillRoad);
            ResetPill(PillEvents);
            ResetPill(PillOfficial);

            // Highlight active filter pill with emerald green
            clickedBtn.BackgroundColor = Color.FromArgb("#047857");
            clickedBtn.TextColor = Colors.White;
            clickedBtn.FontAttributes = FontAttributes.Bold;

            ApplyFilterAndSearch();
        }
    }

    // UI helper: Resets filter pill visual properties back to default unselected style
    private void ResetPill(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

    // Processing engine: Evaluates search keywords and category filters using LINQ
    private void ApplyFilterAndSearch()
    {
        string keyword = TxtSearch.Text?.Trim().ToLower() ?? "";

        var result = _allMasterPosts.AsEnumerable();

        if (_currentFilterCategory != "All")
        {
            result = result.Where(p => p.Category.Equals(_currentFilterCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            result = result.Where(p =>
                p.Title.ToLower().Contains(keyword) ||
                p.Description.ToLower().Contains(keyword) ||
                p.Location.ToLower().Contains(keyword));
        }

        var filteredList = result.ToList();
        LblFeedSummary.Text = $"Showing: \"{_currentFilterCategory}\" ({filteredList.Count} posts)";
        FeedCollectionView.ItemsSource = filteredList;
    }

    // Action handler: Opens CreatePostPage view
    private async void OnCreatePostClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.CreatePostPage));
    }

    // Action handler: Navigates to PostDetailPage with selected item payload
    private async void OnPostSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedPost = e.CurrentSelection.FirstOrDefault() as PostModel;
        if (selectedPost == null) return;

        FeedCollectionView.SelectedItem = null;

        var navData = new Dictionary<string, object>
        {
            { "SelectedPost", selectedPost }
        };

        await Shell.Current.GoToAsync(nameof(Views.PostDetailPage), navData);
    }
}