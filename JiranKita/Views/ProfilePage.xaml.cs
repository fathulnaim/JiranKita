using JiranKita.Services;

namespace JiranKita.Views;

public partial class ProfilePage : ContentPage
{
    // ==============================================================
    // DYNAMIC DATA MAPPING FROM ACTIVE SESSION (ZERO HARDCODED VALUES)
    // ==============================================================
    public string FullNameDisplay => string.IsNullOrWhiteSpace(AppSession.CurrentUserName) ? "Guest Resident" : AppSession.CurrentUserName;
    public string UserInitials => AppSession.CurrentUserInitials;
    public string UnitCommunityDisplay => $"Unit: {AppSession.CurrentUserUnit}, {AppSession.CurrentCommunityName}";
    public string RoleBadgeDisplay
    {
        get
        {
            if (AppSession.IsAdmin)
                return "🛡️ JMB CHAIRMAN / ADMIN";

            if (AppSession.IsVerified) // <-- use your actual AppSession verification check
                return "✓ VERIFIED RESIDENT";

            return "⏳ PENDING VERIFICATION"; // or "UNVERIFIED RESIDENT"
        }
    }

    public string RoleBadgeBgColor => (AppSession.IsAdmin || AppSession.IsVerified) ? "#ECFDF5" : "#FEF3C7";
    public string RoleBadgeBorderColor => (AppSession.IsAdmin || AppSession.IsVerified) ? "#A7F3D0" : "#FDE68A";
    public string RoleBadgeTextColor => (AppSession.IsAdmin || AppSession.IsVerified) ? "#065F46" : "#92400E";

    // Masked MyKad format for PDPA privacy compliance
    public string MaskedIcDisplay
    {
        get
        {
            string ic = AppSession.CurrentUser?.IcNumber ?? "";
            return ic.Length >= 6 ? $"MyKad: {ic.Substring(0, 6)}-XX-XXXX" : "MyKad: Verified";
        }
    }

    // Visibility toggle for JMB Admin panel
    public bool IsAdmin => AppSession.IsAdmin;

    public string UnitSummaryDisplay => $"Registered Unit: {AppSession.CurrentUserUnit}";
    public string OccupantsSummaryDisplay => $"Household Members: {AppSession.CurrentUser?.OccupantsCount ?? 1} registered";
    public string CommunitySummaryDisplay => $"Community: {AppSession.CurrentCommunityName}";

    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Refresh all displayed properties every time the Profile screen is opened
        OnPropertyChanged(nameof(FullNameDisplay));
        OnPropertyChanged(nameof(UserInitials));
        OnPropertyChanged(nameof(UnitCommunityDisplay));
        OnPropertyChanged(nameof(RoleBadgeDisplay));
        OnPropertyChanged(nameof(MaskedIcDisplay));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(UnitSummaryDisplay));
        OnPropertyChanged(nameof(OccupantsSummaryDisplay));
        OnPropertyChanged(nameof(CommunitySummaryDisplay));
    }

    // Navigate to JMB Notice Form
    private async void OnIssueNoticeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.IssueNoticePage));
    }

    // Navigate to JMB Approvals Center
    private async void OnApprovalsQueueClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.ApprovalsQueuePage));
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.AboutPage));
    }

    // ==========================================
    // ACCOUNT LOGOUT
    // ==========================================
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Log Out", "Are you sure you want to log out of your JiranKita account?", "Log Out", "Cancel");
        if (confirm)
        {
            // 1. Clear all active session data from memory
            AppSession.EndSession();

            // 2. Return the user to the Welcome screen
            await Shell.Current.GoToAsync("//WelcomePage");
        }
    }
}