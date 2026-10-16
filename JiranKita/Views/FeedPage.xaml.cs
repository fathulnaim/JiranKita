namespace JiranKita.Views;

public partial class FeedPage : ContentPage
{
    public FeedPage()
    {
        InitializeComponent();
    }

    // Tekan butang + CREATE -> Buka borang CreatePostPage
    private async void OnCreatePostClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.CreatePostPage));
    }

    // Tekan pos Kucing Miko -> Buka PostDetailPage
    private async void OnPost1Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.PostDetailPage));
    }
}