using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class IssueNoticePage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private string _selectedCategory = "Water Disruption";

    public IssueNoticePage()
    {
        InitializeComponent();
    }

    // Handles category chip button selection and updates visual active state
    private void OnCategoryClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            _selectedCategory = btn.Text.Replace("💧 ", "").Replace("🛗 ", "").Replace("🚨 ", "").Replace("📋 ", "");

            ResetCategoryButton(BtnCatWater);
            ResetCategoryButton(BtnCatLift);
            ResetCategoryButton(BtnCatSecurity);
            ResetCategoryButton(BtnCatAgm);

            btn.BackgroundColor = Color.FromArgb("#047857");
            btn.TextColor = Colors.White;
            btn.FontAttributes = FontAttributes.Bold;
        }
    }

    // UI Helper: Resets category button styling back to default unselected state
    private void ResetCategoryButton(Button btn)
    {
        btn.BackgroundColor = Colors.White;
        btn.TextColor = Color.FromArgb("#44403C");
        btn.BorderColor = Color.FromArgb("#D6D3D1");
        btn.BorderWidth = 1;
        btn.FontAttributes = FontAttributes.None;
    }

    // ==============================================================
    // BROADCAST OFFICIAL NOTICE TO FIREBASE IN REAL TIME
    // ==============================================================
    private async void OnBroadcastNoticeClicked(object sender, EventArgs e)
    {
        string title = TxtNoticeTitle.Text?.Trim() ?? "";
        string area = TxtAffectedArea.Text?.Trim() ?? "";
        string desc = TxtNoticeDescription.Text?.Trim() ?? "";

        // Validate mandatory inputs
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(desc))
        {
            await DisplayAlert("Information Required", "Please complete the notice title, affected area, and detailed description.", "OK");
            return;
        }

        string communityCode = AppSession.CurrentCommunityCode;

        try
        {
            // 1. BROADCAST TO URGENT BANNER (HomePage alert card & GIS Map Pin)
            if (ChkUrgentBanner.IsChecked)
            {
                var urgentAlert = new AlertModel
                {
                    Title = title.ToUpper(),
                    Description = $"{desc} (Affected Area: {area})",
                    Category = _selectedCategory.Contains("Water") ? "Hazards" : _selectedCategory.Contains("Security") ? "Security" : "Road",
                    Location = area,
                    Advisory = desc,
                    Distance = "Entire Community",
                    TimeDisplay = "Today",
                    IsActive = true,
                    IsApproved = true, // JMB official notices are automatically approved
                    Severity = "HIGH",
                    ReportedBy = $"{AppSession.CurrentUserName} (JMB)"
                };

                await fbService.SetCommunityUrgentAlertAsync(communityCode, urgentAlert);
            }

            // 2. BROADCAST TO COMMUNITY FEED (Official Category Post)
            if (ChkFeedPost.IsChecked)
            {
                var officialPost = new PostModel
                {
                    Category = "Official",
                    Title = $"[OFFICIAL NOTICE] {title}",
                    Description = $"{desc}\n\n📍 Affected Area: {area}",
                    Location = area,
                    AuthorName = AppSession.CurrentUserName,
                    AuthorUnit = "JMB Management",
                    AuthorInitials = "JMB",
                    IsVerifiedAuthor = true,
                    CreatedAt = DateTime.Now,
                    CommentsCount = 0
                };

                await fbService.CreatePostAsync(communityCode, officialPost);
            }

            await DisplayAlert("Broadcast Successful 📢", "Official JMB notice has been successfully broadcast to the community.", "Done");

            // Navigate back to previous screen
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception error)
        {
            await DisplayAlert("Firebase Error", $"Failed to broadcast notice: {error.Message}", "OK");
        }
    }

    // Input focus listener: Highlights active entry border in green
    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#047857");
            border.StrokeThickness = 2;
        }
    }

    // Input unfocus listener: Resets entry border back to neutral stone gray
    private void OnInputUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#D6D3D1");
            border.StrokeThickness = 1;
        }
    }
}