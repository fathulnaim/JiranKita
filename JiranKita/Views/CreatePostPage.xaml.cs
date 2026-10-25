using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class CreatePostPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private string selectedCategory = "Lost & Found"; // Default
    private string? _attachedPhotoPath = null;

    public CreatePostPage()
    {
        InitializeComponent();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // PEMILIHAN KATEGORI
    private void OnCategoryClicked(object sender, EventArgs e)
    {
        if (sender is Button clickedBtn)
        {
            selectedCategory = clickedBtn.Text.Replace("&amp;", "&");

            // Reset semua butang ke warna putih
            ResetButton(BtnCatLostFound);
            ResetButton(BtnCatRoad);
            ResetButton(BtnCatEvents);
            ResetButton(BtnCatOfficial);
            ResetButton(BtnCatEmergency);

            // Butang yang dipilih bertukar jadi jingga pekat
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

    private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        int length = e.NewTextValue?.Length ?? 0;
        LblTitleCount.Text = $"{length} / 70 characters";
    }

    // GAMBAR (KAMERA & GALERI)
    private async void OnCameraClicked(object sender, EventArgs e)
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null) SetPhoto(photo.FullPath);
        }
    }

    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        FileResult? photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo != null) SetPhoto(photo.FullPath);
    }

    private void SetPhoto(string fullPath)
    {
        _attachedPhotoPath = fullPath;
        ImgPreview.Source = ImageSource.FromFile(fullPath);
        AttachedPhotoCard.IsVisible = true;
    }

    private void OnRemovePhotoClicked(object sender, EventArgs e)
    {
        _attachedPhotoPath = null;
        ImgPreview.Source = null;
        AttachedPhotoCard.IsVisible = false;
    }

    // HANTAR POS KE FIREBASE
    private async void OnPublishClicked(object sender, EventArgs e)
    {
        string title = TxtTitle.Text?.Trim() ?? "";
        string desc = TxtDescription.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(desc))
        {
            await DisplayAlert("Maklumat Tidak Lengkap", "Sila masukkan tajuk dan huraian hantaran anda.", "OK");
            return;
        }

        var newPost = new PostModel
        {
            Category = selectedCategory,
            Title = title,
            Description = desc,
            Location = "Block C Lobby",
            AuthorName = "Muhammad Fathul",
            AuthorUnit = "Unit B-12-04",
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
            // Hantar pos baharu terus masuk ke bubble komuniti semasa!
            await fbService.CreatePostAsync(AppSession.CurrentCommunityCode, newPost);
            await DisplayAlert("Berjaya!", "Hantaran anda telah diterbitkan kepada komuniti.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat Firebase", $"Gagal menerbitkan: {error.Message}", "OK");
        }
    }
}