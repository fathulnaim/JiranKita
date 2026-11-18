using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class CreatePostPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private string _selectedCategory = "Lost & Found";
    private string _selectedLocation = string.Empty;
    private string? _attachedImageBase64 = null;
    private bool _isPublishing = false;

    public CreatePostPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (string.IsNullOrWhiteSpace(_selectedLocation))
        {
            _selectedLocation = !string.IsNullOrWhiteSpace(AppSession.CurrentCommunityName)
                ? AppSession.CurrentCommunityName
                : "Community Area";

            LblLocation.Text = $"📍 {_selectedLocation}";
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnCategoryClicked(object sender, EventArgs e)
    {
        if (sender is Button clickedBtn)
        {
            _selectedCategory = clickedBtn.Text.Replace("&amp;", "&");

            ResetButton(BtnCatLostFound);
            ResetButton(BtnCatRoad);
            ResetButton(BtnCatEvents);
            ResetButton(BtnCatOfficial);
            ResetButton(BtnCatEmergency);

            clickedBtn.BackgroundColor = Color.FromArgb("#D97706");
            clickedBtn.TextColor = Colors.White;
            clickedBtn.FontAttributes = FontAttributes.Bold;
        }
    }

    private void ResetButton(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

    private async void OnChangeLocationTapped(object sender, TappedEventArgs e)
    {
        string unitLocation = !string.IsNullOrWhiteSpace(AppSession.CurrentUserUnit)
            ? $"Unit {AppSession.CurrentUserUnit}, {AppSession.CurrentCommunityName}"
            : AppSession.CurrentCommunityName;

        string action = await DisplayActionSheet(
            "Select Post Location",
            "Cancel",
            null,
            $"🏠 {unitLocation}",
            $"🏘️ {AppSession.CurrentCommunityName} (General)",
            "🚧 Main Guardhouse / Entrance",
            "🏢 Clubhouse / Community Hall",
            "✏️ Enter Custom Location..."
        );

        if (string.IsNullOrWhiteSpace(action) || action == "Cancel")
            return;

        if (action == "✏️ Enter Custom Location...")
        {
            string custom = await DisplayPromptAsync(
                "Custom Location",
                "Enter the specific spot or landmark within your neighbourhood:",
                "Save",
                "Cancel",
                placeholder: "e.g. Level 2 Visitor Parking, Block B",
                initialValue: _selectedLocation
            );

            if (!string.IsNullOrWhiteSpace(custom))
            {
                _selectedLocation = custom.Trim();
                LblLocation.Text = $"📍 {_selectedLocation}";
            }
        }
        else
        {
            _selectedLocation = action.Substring(action.IndexOf(' ') + 1).Trim();
            LblLocation.Text = $"📍 {_selectedLocation}";
        }
    }

    private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        int length = e.NewTextValue?.Length ?? 0;
        LblTitleCount.Text = $"{length} / 70 characters";
    }

    // Launches device camera and converts photo to Base64
    private async void OnCameraClicked(object sender, EventArgs e)
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null) await SetPhotoAsync(photo);
        }
    }

    // Opens device photo gallery picker and converts photo to Base64
    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        FileResult? photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo != null) await SetPhotoAsync(photo);
    }

    // Reads image stream into Base64 so it persists permanently in Firebase
    private async Task SetPhotoAsync(FileResult photo)
    {
        using Stream stream = await photo.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        byte[] imageBytes = memoryStream.ToArray();
        _attachedImageBase64 = Convert.ToBase64String(imageBytes);

        ImgPreview.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
        AttachedPhotoCard.IsVisible = true;
    }

    private void OnRemovePhotoClicked(object sender, EventArgs e)
    {
        _attachedImageBase64 = null;
        ImgPreview.Source = null;
        AttachedPhotoCard.IsVisible = false;
    }

    private async void OnPublishClicked(object sender, EventArgs e)
    {
        if (_isPublishing) return;

        string title = TxtTitle.Text?.Trim() ?? string.Empty;
        string desc = TxtDescription.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(desc))
        {
            await DisplayAlert("Incomplete Information", "Please enter both a title and detailed description for your post.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(AppSession.CurrentCommunityCode))
        {
            await DisplayAlert("No Active Community", "Please join or select a neighbourhood community before publishing a post.", "OK");
            return;
        }

        _isPublishing = true;
        if (sender is Button publishBtn) publishBtn.IsEnabled = false;

        string formattedUnit = string.IsNullOrWhiteSpace(AppSession.CurrentUserUnit)
            ? "Resident"
            : (AppSession.CurrentUserUnit.StartsWith("Unit ", StringComparison.OrdinalIgnoreCase)
                ? AppSession.CurrentUserUnit
                : $"Unit {AppSession.CurrentUserUnit}");

        var newPost = new PostModel
        {
            Category = _selectedCategory,
            Title = title,
            Description = desc,
            Location = _selectedLocation,
            AuthorName = AppSession.CurrentUserName,
            AuthorUnit = formattedUnit,
            AuthorInitials = AppSession.CurrentUserInitials,
            AuthorPhone = AppSession.CurrentUserPhone,
            IsVerifiedAuthor = AppSession.CurrentUser?.IsVerified ?? false,
            CreatedAt = DateTime.Now,
            CommentsCount = 0,
            ImageUrl = _attachedImageBase64 ?? string.Empty, // Stores actual Base64 image data in Firebase
            AllowDirectContact = ChkAllowContact.IsChecked
        };

        try
        {
            await _firebaseService.CreatePostAsync(AppSession.CurrentCommunityCode, newPost);
            await DisplayAlert("Success!", "Your post has been published to the community feed.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception error)
        {
            await DisplayAlert("Firebase Error", $"Failed to publish post: {error.Message}", "OK");
        }
        finally
        {
            _isPublishing = false;
            if (sender is Button btn) btn.IsEnabled = true;
        }
    }
}