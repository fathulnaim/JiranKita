using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class CreatePostPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private string _selectedCategory = "Lost & Found"; // Default active category
    private string _selectedLocation = string.Empty;   // Stores clean location string without UI emoji
    private string? _attachedPhotoPath = null;
    private bool _isPublishing = false;                // Prevents double-tap duplicate submissions

    // Constructor: Initializes layout controls
    public CreatePostPage()
    {
        InitializeComponent();
    }

    // Lifecycle hook: Populates dynamic default community location when the page opens
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Set default location from active AppSession only if not already customized by the user
        if (string.IsNullOrWhiteSpace(_selectedLocation))
        {
            _selectedLocation = !string.IsNullOrWhiteSpace(AppSession.CurrentCommunityName)
                ? AppSession.CurrentCommunityName
                : "Community Area";

            LblLocation.Text = $"📍 {_selectedLocation}";
        }
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

    // Location selector handler: Lets resident choose a preset community landmark or enter a custom spot
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
            // Strip leading emoji prefix so clean location text is stored in Firebase
            _selectedLocation = action.Substring(action.IndexOf(' ') + 1).Trim();
            LblLocation.Text = $"📍 {_selectedLocation}";
        }
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
        if (_isPublishing) return;

        string title = TxtTitle.Text?.Trim() ?? string.Empty;
        string desc = TxtDescription.Text?.Trim() ?? string.Empty;

        // 1. Validate required post inputs
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(desc))
        {
            await DisplayAlert("Incomplete Information", "Please enter both a title and detailed description for your post.", "OK");
            return;
        }

        // 2. Ensure active community session exists before writing to Firebase
        if (string.IsNullOrWhiteSpace(AppSession.CurrentCommunityCode))
        {
            await DisplayAlert("No Active Community", "Please join or select a neighbourhood community before publishing a post.", "OK");
            return;
        }

        _isPublishing = true;
        if (sender is Button publishBtn) publishBtn.IsEnabled = false;

        // Normalize unit display format cleanly (prevents duplicate "Unit Unit ...")
        string formattedUnit = string.IsNullOrWhiteSpace(AppSession.CurrentUserUnit)
            ? "Resident"
            : (AppSession.CurrentUserUnit.StartsWith("Unit ", StringComparison.OrdinalIgnoreCase)
                ? AppSession.CurrentUserUnit
                : $"Unit {AppSession.CurrentUserUnit}");

        // 3. Build PostModel dynamically from active AppSession (zero hardcoded data)
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
            ImageUrl = _attachedPhotoPath ?? string.Empty,
            AllowDirectContact = ChkAllowContact.IsChecked
        };

        try
        {
            // 4. Publish new post entry directly into current community code scope
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