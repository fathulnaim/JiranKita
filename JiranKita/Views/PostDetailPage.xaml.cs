using JiranKita.Models;
using JiranKita.Services;
using System.Collections.ObjectModel;

namespace JiranKita.Views;

[QueryProperty(nameof(Post), "SelectedPost")]
public partial class PostDetailPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private double _currentScale = 1;
    private double _startScale = 1;
    private double _xOffset = 0;
    private double _yOffset = 0;

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

    // Fetches comment thread for the active post key from Firebase real-time database
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
            await DisplayAlert("Error", $"Failed to load comments: {error.Message}", "OK");
        }
    }

    // Submits a new reply comment under the current post thread
    private async void OnSendReplyClicked(object sender, EventArgs e)
    {
        string replyText = TxtReply.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(replyText)) return;

        TxtReply.Text = string.Empty;

        string unitDisplay = AppSession.CurrentUserUnit.StartsWith("Unit ", StringComparison.OrdinalIgnoreCase)
            ? AppSession.CurrentUserUnit
            : $"Unit {AppSession.CurrentUserUnit}";

        var newComment = new CommentModel
        {
            AuthorName = AppSession.CurrentUserName,
            AuthorUnit = unitDisplay,
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
            await DisplayAlert("Error", $"Failed to send comment: {error.Message}", "OK");
        }
    }

    // Displays contact options modal sheet to start direct chat or trigger phone dialer
    private async void OnContactOwnerClicked(object sender, EventArgs e)
    {
        if (Post.AuthorName.Equals(AppSession.CurrentUserName, StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlert("Info", "This is your own post.", "OK");
            return;
        }

        string choice = await DisplayActionSheet(
            $"Contact {Post.AuthorName}",
            "Cancel",
            null,
            "💬 Direct Message",
            "📞 Official Phone Call"
        );

        if (choice == "💬 Direct Message")
        {
            string directRoomId = AppSession.GetDirectRoomId(AppSession.CurrentUserName, Post.AuthorName);

            var navParams = new Dictionary<string, object>
            {
                { "RoomId", directRoomId },
                { "TargetName", Post.AuthorName },
                { "TargetUnit", Post.AuthorUnit }
            };

            await Shell.Current.GoToAsync(nameof(Views.ChatRoomPage), navParams);
        }
        else if (choice == "📞 Official Phone Call")
        {
            if (string.IsNullOrWhiteSpace(Post.AuthorPhone))
            {
                await DisplayAlert("Unavailable", "Phone number is not available for this resident.", "OK");
                return;
            }

            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open(Post.AuthorPhone);
            }
            else
            {
                await DisplayAlert("Not Supported", "Phone dialer is not supported on this device.", "OK");
            }
        }
    }

    private void OnPostImageTapped(object sender, TappedEventArgs e)
    {
        string? imageUrl = e.Parameter as string ?? Post?.ImageUrl;
        if (string.IsNullOrWhiteSpace(imageUrl)) return;

        ResetImageZoom();
        ImgFullscreen.Source = imageUrl;
        ImageZoomOverlay.IsVisible = true;
    }

    private void OnCloseImageZoomClicked(object sender, EventArgs e)
    {
        ImageZoomOverlay.IsVisible = false;
        ImgFullscreen.Source = null;
        ResetImageZoom();
    }

    private void ResetImageZoom()
    {
        _currentScale = 1;
        _startScale = 1;
        _xOffset = 0;
        _yOffset = 0;
        ImgFullscreen.Scale = 1;
        ImgFullscreen.TranslationX = 0;
        ImgFullscreen.TranslationY = 0;
    }

    private void OnImagePinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _startScale = ImgFullscreen.Scale;
        }
        else if (e.Status == GestureStatus.Running)
        {
            _currentScale += (e.Scale - 1) * _startScale;
            _currentScale = Math.Clamp(_currentScale, 1, 4);
            ImgFullscreen.Scale = _currentScale;

            if (_currentScale <= 1.01)
            {
                ImgFullscreen.TranslationX = 0;
                ImgFullscreen.TranslationY = 0;
                _xOffset = 0;
                _yOffset = 0;
            }
        }
        else if (e.Status == GestureStatus.Completed)
        {
            _xOffset = ImgFullscreen.TranslationX;
            _yOffset = ImgFullscreen.TranslationY;
        }
    }

    private void OnImagePanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (_currentScale <= 1) return;

        if (e.StatusType == GestureStatus.Running)
        {
            double maxOffsetX = (ImgFullscreen.Width * (_currentScale - 1)) / 2;
            double maxOffsetY = (ImgFullscreen.Height * (_currentScale - 1)) / 2;

            ImgFullscreen.TranslationX = Math.Clamp(_xOffset + e.TotalX, -maxOffsetX, maxOffsetX);
            ImgFullscreen.TranslationY = Math.Clamp(_yOffset + e.TotalY, -maxOffsetY, maxOffsetY);
        }
        else if (e.StatusType == GestureStatus.Completed)
        {
            _xOffset = ImgFullscreen.TranslationX;
            _yOffset = ImgFullscreen.TranslationY;
        }
    }

    private void OnImageDoubleTapped(object sender, TappedEventArgs e)
    {
        if (_currentScale > 1)
        {
            ResetImageZoom();
        }
        else
        {
            _currentScale = 2.5;
            ImgFullscreen.Scale = _currentScale;
        }
    }
}