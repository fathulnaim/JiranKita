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
            var commentList = await fbService.GetCommentsAsync(AppSession.CurrentCommunityCode, Post.FirebaseKey);
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
            await fbService.AddCommentAsync(AppSession.CurrentCommunityCode, Post.FirebaseKey, newComment);
            Comments.Add(newComment);
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat", $"Gagal menghantar komen: {error.Message}", "OK");
        }
    }

    private async void OnContactOwnerClicked(object sender, EventArgs e)
    {
        string choice = await DisplayActionSheet(
            $"Hubungi {Post.AuthorName}",
            "Batal",
            null,
            "💬 Mesej Peribadi (1-on-1 Chat)",
            "📞 Panggilan Telefon Rasmi"
        );

        if (choice == "💬 Mesej Peribadi (1-on-1 Chat)")
        {
            string directRoomId = AppSession.GetDirectRoomId(AppSession.CurrentUserName, Post.AuthorName);

            var navParams = new Dictionary<string, object>
            {
                { "RoomId", directRoomId },
                { "TargetName", Post.AuthorName },
                { "TargetUnit", Post.AuthorUnit }
            };

            // SELESAI CRASH: Lompat ke ChatRoomPage!
            await Shell.Current.GoToAsync(nameof(Views.ChatRoomPage), navParams);
        }
        else if (choice == "📞 Panggilan Telefon Rasmi")
        {
            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open("+60123456789");
            }
        }
    }
}