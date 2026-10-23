using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class FeedPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private List<PostModel> _allMasterPosts = new();
    private string _currentFilterCategory = "All";

    public FeedPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFeedPostsFromFirebase();
    }

    private async Task LoadFeedPostsFromFirebase()
    {
        try
        {
            _allMasterPosts = await fbService.GetAllPostsAsync();
            ApplyFilterAndSearch();
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat", $"Gagal memuatkan pos: {error.Message}", "OK");
        }
    }

    // CARIAN TEKS
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilterAndSearch();
    }

    // PENAPIS KATEGORI
    private void OnFilterPillClicked(object sender, EventArgs e)
    {
        if (sender is Button clickedBtn)
        {
            string cat = clickedBtn.Text.Replace("&amp;", "&");
            if (cat.Contains("Road")) cat = "Road & Maintenance";
            _currentFilterCategory = cat;

            // Reset semua butang penapis ke warna putih
            ResetPill(PillAll);
            ResetPill(PillLost);
            ResetPill(PillRoad);
            ResetPill(PillEvents);
            ResetPill(PillOfficial);

            // Butang aktif bertukar jadi hijau
            clickedBtn.BackgroundColor = Color.FromArgb("#047857");
            clickedBtn.TextColor = Colors.White;
            clickedBtn.FontAttributes = FontAttributes.Bold;

            ApplyFilterAndSearch();
        }
    }

    private void ResetPill(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

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

    private async void OnCreatePostClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.CreatePostPage));
    }

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