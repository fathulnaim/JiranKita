namespace JiranKita.Views;

public partial class CreatePostPage : ContentPage
{
    public CreatePostPage()
    {
        InitializeComponent();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnCameraClicked(object sender, EventArgs e)
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            await MediaPicker.Default.CapturePhotoAsync();
        }
    }

    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        await MediaPicker.Default.PickPhotoAsync();
    }

    private async void OnPublishClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Berjaya!", "Hantaran anda telah diterbitkan kepada komuniti.", "OK");
        await Shell.Current.GoToAsync("..");
    }
}