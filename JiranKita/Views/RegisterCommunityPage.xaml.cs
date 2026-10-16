namespace JiranKita.Views;

public partial class RegisterCommunityPage : ContentPage
{
    public RegisterCommunityPage()
    {
        InitializeComponent();
    }

    private async void OnUploadCertificateClicked(object sender, EventArgs e)
    {
        FileResult file = await MediaPicker.Default.PickPhotoAsync();
        if (file != null)
        {
            LblCertFileName.Text = $"Uploaded: {file.FileName}";
        }
    }

    private async void OnSubmitCommunityClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCommunityName.Text) || string.IsNullOrWhiteSpace(TxtCobNumber.Text))
        {
            await DisplayAlert("Ralat", "Sila lengkapkan nama komuniti dan nombor pendaftaran COB.", "OK");
            return;
        }

        await DisplayAlert(
            "Permohonan Diterima",
            $"Pendaftaran untuk '{TxtCommunityName.Text}' telah dihantar. Pihak pentadbir JiranKita akan mengesahkan pendaftaran ini dalam masa 24-48 jam.",
            "Selesai"
        );

        await Shell.Current.GoToAsync("..");
    }

    // Indikator Fokus Hijau
    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
        }
    }

    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }
}