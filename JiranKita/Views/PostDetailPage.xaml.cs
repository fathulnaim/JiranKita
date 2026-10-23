using System.Collections.ObjectModel;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

[QueryProperty(nameof(Post), "SelectedPost")]
public partial class PostDetailPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();

    private PostModel _post = new PostModel();
    public PostModel Post
    {
        get => _post;
        set
        {
            _post = value;
            OnPropertyChanged();
            LoadCommentsFromFirebase();
        }
    }

    public ObservableCollection<CommentModel> Comments { get; set; } = new ObservableCollection<CommentModel>();

    public PostDetailPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void LoadCommentsFromFirebase()
    {
        if (string.IsNullOrEmpty(Post.FirebaseKey)) return;

        try
        {
            var commentList = await fbService.GetCommentsAsync(Post.FirebaseKey);
            Comments.Clear();
            foreach (var comment in commentList)
            {
                Comments.Add(comment);
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat", $"Gagal memuatkan komen: {error.Message}", "OK");
        }
    }

    private async void OnSendReplyClicked(object sender, EventArgs e)
    {
        string replyText = TxtReply.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(replyText)) return;

        TxtReply.Text = string.Empty;

        var newComment = new CommentModel
        {
            AuthorName = "Muhammad Fathul",
            AuthorUnit = "Unit B-12-04",
            CommentText = replyText,
            CreatedAt = DateTime.Now
        };

        try
        {
            await fbService.AddCommentAsync(Post.FirebaseKey, newComment);
            Comments.Add(newComment);
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat", $"Gagal menghantar komen: {error.Message}", "OK");
        }
    }

    private async void OnContactOwnerClicked(object sender, EventArgs e)
    {
        if (PhoneDialer.Default.IsSupported)
        {
            PhoneDialer.Default.Open("+60123456789");
        }
        else
        {
            await DisplayAlert("Hubungi", "Panggilan ke: +60 12-345 6789", "OK");
        }
    }
}