using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class CreatePostPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private string _selectedCategory = "Lost & Found"; // Default category
    private string? _attachedPhotoPath = null;

    // Constructor: Initializes layout controls
    public CreatePostPage()
    {
        InitializeComponent();
    }

    // Navigation handler: Pops current page back to previous stack view
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // Category selection handler: Sets active category string and updates pill button styling
    private void OnCategoryClicked(object sender, EventArgs e)
    {
        if (sender is Button clickedBtn)
        {
            _selectedCategory = clickedBtn.Text.Replace("&amp;", "&");

            // Reset all category button styles back to default white
            ResetButton(BtnCatLostFound);
            ResetButton(BtnCatRoad);
            ResetButton(BtnCatEvents);
            ResetButton(BtnCatOfficial);
            ResetButton(BtnCatEmergency);

            // Apply selected visual styling (amber orange background, white text)
            clickedBtn.BackgroundColor = Color.FromArgb("#D97706");
            clickedBtn.TextColor = Colors.White;
            clickedBtn.FontAttributes = FontAttributes.Bold;
        }
    }

    // UI helper: Resets a category button back to unselected visual state
    private void ResetButton(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

    // Input listener: Dynamically tracks title input character count (Max: 70)
    private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        int length = e.NewTextValue?.Length ?? 0;
        LblTitleCount.Text = $"{length} / 70 characters";
    }

    // Media picker handler: Launches device camera to capture photo
    private async void OnCameraClicked(object sender, EventArgs e)
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null) SetPhoto(photo.FullPath);
        }
    }

    // Media picker handler: Opens device photo gallery picker
    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        FileResult? photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo != null) SetPhoto(photo.FullPath);
    }

    // Image state helper: Saves selected photo path and displays image preview card
    private void SetPhoto(string fullPath)
    {
        _attachedPhotoPath = fullPath;
        ImgPreview.Source = ImageSource.FromFile(fullPath);
        AttachedPhotoCard.IsVisible = true;
    }

    // Image state helper: Clears active photo attachment and hides preview card
    private void OnRemovePhotoClicked(object sender, EventArgs e)
    {
        _attachedPhotoPath = null;
        ImgPreview.Source = null;
        AttachedPhotoCard.IsVisible = false;
    }

    // Core action handler: Validates post inputs, instantiates PostModel, and publishes to Firebase
    private async void OnPublishClicked(object sender, EventArgs e)
    {
        string title = TxtTitle.Text?.Trim() ?? "";
        string desc = TxtDescription.Text?.Trim() ?? "";

        // Form validation
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(desc))
        {
            await DisplayAlert("Incomplete Information", "Please enter both title and detailed description for your post.", "OK");
            return;
        }

        var newPost = new PostModel
        {
            Category = _selectedCategory,
            Title = title,
            Description = desc,
            Location = "Block C Lobby",
            AuthorName = AppSession.CurrentUserName ?? "Muhammad Fathul",
            AuthorUnit = AppSession.CurrentUserUnit ?? "Unit B-12-04",
            AuthorInitials = "FN",
            IsVerifiedAuthor = true,
            CreatedAt = DateTime.Now,
            CommentsCount = 0,
            ImageUrl = _attachedPhotoPath ?? "",
            AllowDirectContact = ChkAllowContact.IsChecked,
            IsUrgentBroadcast = ChkUrgentSiren.IsChecked
        };

        try
        {
            // Publish new post entry directly into current community code scope
            await _firebaseService.CreatePostAsync(AppSession.CurrentCommunityCode, newPost);
            await DisplayAlert("Success!", "Your post has been published to the community feed.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception error)
        {
            await DisplayAlert("Firebase Error", $"Failed to publish post: {error.Message}", "OK");
        }
    }
}