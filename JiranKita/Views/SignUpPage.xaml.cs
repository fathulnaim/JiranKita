using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class SignUpPage : ContentPage
{
    private int _currentStep = 1;
    private int _occupantsCount = 1;
    private string _selectedOwnership = "Owner";
    private int _vehicleCount = 0;

    public SignUpPage()
    {
        InitializeComponent();
        UpdateStepUI();

        // Tambah 2 kenderaan contoh
        AddVehicleItem("WXX 8182", "Proton X50");
        AddVehicleItem("VAA 2341", "Perodua Bezza");
    }

    // ==========================================
    // 1. INPUT FOCUS INDICATOR (GREEN BORDER)
    // ==========================================
    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry input && input.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
        }
    }

    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry input && input.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }

    // ==========================================
    // 2. RESIDENCY OWNERSHIP SELECTION
    // ==========================================
    private void OnSelectOwnership(object sender, TappedEventArgs e)
    {
        _selectedOwnership = e.Parameter?.ToString() ?? "Owner";

        // Reset all options to default gray
        ResetOwnershipCard(CardOwner, LblOwner, "Owner-Occupier (Pemilik)");
        ResetOwnershipCard(CardTenant, LblTenant, "Tenant (Penyewa)");
        ResetOwnershipCard(CardFamily, LblFamily, "Family Dependant");
        ResetOwnershipCard(CardJmb, LblJmb, "JMB Committee");

        // Highlight selected option in green
        if (_selectedOwnership == "Owner") SetOwnershipActive(CardOwner, LblOwner);
        else if (_selectedOwnership == "Tenant") SetOwnershipActive(CardTenant, LblTenant);
        else if (_selectedOwnership == "Family") SetOwnershipActive(CardFamily, LblFamily);
        else if (_selectedOwnership == "JMB") SetOwnershipActive(CardJmb, LblJmb);
    }

    private void SetOwnershipActive(Border card, Label label)
    {
        card.Stroke = Color.FromArgb("#047857");
        card.StrokeThickness = 2;
        card.BackgroundColor = Color.FromArgb("#ECFDF5");
        label.TextColor = Color.FromArgb("#065F46");
        label.FontAttributes = FontAttributes.Bold;
    }

    private void ResetOwnershipCard(Border card, Label label, string text)
    {
        card.Stroke = Color.FromArgb("#E7E5E4");
        card.StrokeThickness = 1;
        card.BackgroundColor = Color.FromArgb("#FAFAF9");
        label.TextColor = Color.FromArgb("#44403C");
        label.FontAttributes = FontAttributes.None;
    }

    // ==========================================
    // 3. DYNAMIC VEHICLES (+ Add / Delete)
    // ==========================================
    private void OnAddVehicleClicked(object sender, EventArgs e)
    {
        AddVehicleItem("", "");
    }

    private void AddVehicleItem(string plateNo, string model)
    {
        _vehicleCount++;
        int index = _vehicleCount;

        var card = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Color.FromArgb("#E7E5E4"),
            BackgroundColor = Color.FromArgb("#FAFAF9"),
            Padding = 10
        };

        var stack = new VerticalStackLayout { Spacing = 6 };

        var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        header.Add(new Label { Text = $"Vehicle #{index} (CAR)", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#44403C") }, 0, 0);

        var btnDelete = new Label { Text = "🗑️", FontSize = 12, VerticalOptions = LayoutOptions.Center };
        btnDelete.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => VehiclesContainer.Children.Remove(card))
        });
        header.Add(btnDelete, 1, 0);

        var inputs = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 6 };

        var borderPlate = new Border { StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }, Stroke = Color.FromArgb("#D6D3D1"), BackgroundColor = Colors.White, Padding = new Thickness(8, 2) };
        var entryPlate = new Entry { Text = plateNo, Placeholder = "Plate No.", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#065F46") };
        entryPlate.Focused += OnInputFocused;
        entryPlate.Unfocused += OnInputUnfocused;
        borderPlate.Content = entryPlate;

        var borderModel = new Border { StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }, Stroke = Color.FromArgb("#D6D3D1"), BackgroundColor = Colors.White, Padding = new Thickness(8, 2) };
        var entryModel = new Entry { Text = model, Placeholder = "Car Model", FontSize = 12, TextColor = Color.FromArgb("#44403C") };
        entryModel.Focused += OnInputFocused;
        entryModel.Unfocused += OnInputUnfocused;
        borderModel.Content = entryModel;

        inputs.Add(borderPlate, 0, 0);
        inputs.Add(borderModel, 1, 0);

        stack.Add(header);
        stack.Add(inputs);
        card.Content = stack;

        VehiclesContainer.Children.Add(card);
    }

    // ==========================================
    // 4. STEP WIZARD NAVIGATION & VALIDATION
    // ==========================================
    private async void OnStep1NextClicked(object sender, EventArgs e)
    {
        // Semak kata laluan DAHULU sebelum benarkan lompat ke Step 2
        if (string.IsNullOrWhiteSpace(TxtPassword.Text) || TxtPassword.Text.Length < 6)
        {
            await DisplayAlert("Kata Laluan Diperlukan", "Sila cipta kata laluan sekurang-kurangnya 6 aksara.", "OK");
            return;
        }

        _currentStep = 2;
        UpdateStepUI();
    }

    private void OnStep2NextClicked(object sender, EventArgs e)
    {
        _currentStep = 3;
        UpdateStepUI();
    }

    private void OnStep3NextClicked(object sender, EventArgs e)
    {
        // Auto-update maklumat ringkasan di Langkah 4
        LblSummaryName.Text = TxtFullName.Text;
        LblSummaryUnit.Text = $"{TxtUnitNo.Text} ({PickerPrecinct.SelectedItem})";

        _currentStep = 4;
        UpdateStepUI();
    }

    private async void OnStepBackClicked(object sender, EventArgs e)
    {
        if (_currentStep > 1)
        {
            _currentStep--;
            UpdateStepUI();
        }
        else
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private void UpdateStepUI()
    {
        Step1Container.IsVisible = (_currentStep == 1);
        Step2Container.IsVisible = (_currentStep == 2);
        Step3Container.IsVisible = (_currentStep == 3);
        Step4Container.IsVisible = (_currentStep == 4);

        SetStepCircle(StepCircle1, StepText1, StepLabel1, 1);
        SetStepCircle(StepCircle2, StepText2, StepLabel2, 2);
        SetStepCircle(StepCircle3, StepText3, StepLabel3, 3);
        SetStepCircle(StepCircle4, StepText4, StepLabel4, 4);
    }

    private void SetStepCircle(Border circle, Label text, Label label, int stepNumber)
    {
        Color emeraldDark = Color.FromArgb("#047857");
        Color emeraldLight = Color.FromArgb("#ECFDF5");
        Color emeraldBorder = Color.FromArgb("#A7F3D0");
        Color stoneGray = Color.FromArgb("#78716C");

        if (_currentStep == stepNumber)
        {
            circle.BackgroundColor = emeraldDark;
            circle.Stroke = Colors.Transparent;
            text.Text = stepNumber.ToString();
            text.TextColor = Colors.White;
            label.TextColor = emeraldDark;
            label.FontAttributes = FontAttributes.Bold;
        }
        else if (_currentStep > stepNumber)
        {
            circle.BackgroundColor = emeraldLight;
            circle.Stroke = emeraldBorder;
            text.Text = "✓";
            text.TextColor = emeraldDark;
            label.TextColor = emeraldDark;
            label.FontAttributes = FontAttributes.None;
        }
        else
        {
            circle.BackgroundColor = Colors.White;
            circle.Stroke = Color.FromArgb("#D6D3D1");
            text.Text = stepNumber.ToString();
            text.TextColor = stoneGray;
            label.TextColor = stoneGray;
            label.FontAttributes = FontAttributes.None;
        }
    }

    private void OnIncrementOccupants(object sender, EventArgs e)
    {
        _occupantsCount++;
        LblOccupantsCount.Text = _occupantsCount.ToString();
    }

    private void OnDecrementOccupants(object sender, EventArgs e)
    {
        if (_occupantsCount > 1)
        {
            _occupantsCount--;
            LblOccupantsCount.Text = _occupantsCount.ToString();
        }
    }

    private async void OnUploadDocumentClicked(object sender, EventArgs e)
    {
        FileResult photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo != null)
        {
            LblDocFileName.Text = $"Attached: {photo.FileName}";
        }
    }

    // ==========================================
    // 5. FINAL SUBMIT TO FIREBASE
    // ==========================================
    private async void OnFinalSubmitClicked(object sender, EventArgs e)
    {
        if (!ChkPdpa.IsChecked)
        {
            await DisplayAlert("Persetujuan Diperlukan", "Sila tandakan persetujuan Akta PDPA 2010 sebelum meneruskan.", "OK");
            return;
        }

        // Kumpul semua data borang 4-langkah
        var newUser = new UserModel
        {
            FullName = TxtFullName.Text?.Trim() ?? "",
            IcNumber = TxtICNumber.Text?.Trim() ?? "",
            Email = TxtEmail.Text?.Trim() ?? "",
            Phone = TxtPhone.Text?.Trim() ?? "",
            Password = TxtPassword.Text?.Trim() ?? "",

            HousingArea = PickerPrecinct.SelectedItem?.ToString() ?? "Taman Seri Kembangan",
            Block = TxtBlock.Text?.Trim() ?? "",
            Level = TxtLevel.Text?.Trim() ?? "",
            UnitNo = TxtUnitNo.Text?.Trim() ?? "",
            OwnershipStatus = _selectedOwnership,

            OccupantsCount = _occupantsCount,
            EmergencyName = TxtEmergName.Text?.Trim() ?? "",
            EmergencyRelation = TxtEmergRel.Text?.Trim() ?? "",
            EmergencyPhone = TxtEmergPhone.Text?.Trim() ?? ""
        };

        try
        {
            // Hantar data pendaftaran terus ke Firebase
            var fbService = new FirebaseService();
            await fbService.RegisterUserAsync(newUser);

            await DisplayAlert("Pendaftaran Berjaya", "Permohonan pendaftaran residensi anda telah dihantar kepada JMB untuk semakan & kelulusan.", "Teruskan");

            // Bawa pengguna ke skrin imbas QR / semak komuniti
            await Shell.Current.GoToAsync("FindNeighbourhoodPage");
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat Firebase", $"Gagal mendaftar: {error.Message}", "OK");
        }
    }
}