using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class ApprovalsQueuePage : ContentPage
{
    private readonly FirebaseService fbService = new();
    private List<UserModel> _pendingResidents = new();
    private List<AlertModel> _pendingAlerts = new();
    private readonly string _initialTab;

    // Default constructor (starts on Residents tab)
    public ApprovalsQueuePage() : this("residents")
    {
    }

    // Overloaded constructor allowing direct selection ("residents" or "alerts")
    public ApprovalsQueuePage(string initialTab)
    {
        InitializeComponent();
        _initialTab = initialTab?.ToLowerInvariant() ?? "residents";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Apply starting tab selection
        if (_initialTab == "alerts")
        {
            ShowAlertsTab();
        }
        else
        {
            ShowResidentsTab();
        }

        await LoadAllPendingData();
    }

    private async Task LoadAllPendingData()
    {
        try
        {
            // 1. Fetch pending resident applicants
            _pendingResidents = await fbService.GetPendingResidentsAsync();
            CvPendingResidents.ItemsSource = _pendingResidents;
            LblResidentsCount.Text = $"Showing {_pendingResidents.Count} pending resident applicant(s)";

            // 2. Fetch pending emergency alert reports
            _pendingAlerts = await fbService.GetPendingAlertsAsync(AppSession.CurrentCommunityCode);
            CvPendingAlerts.ItemsSource = _pendingAlerts;
            LblAlertsCount.Text = $"Showing {_pendingAlerts.Count} pending incident alert(s)";
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", $"Failed to load data: {error.Message}", "OK");
        }
    }

    // ==========================================
    // TAB SWITCHER LOGIC
    // ==========================================
    private void OnTabResidentsClicked(object sender, EventArgs e) => ShowResidentsTab();
    private void OnTabAlertsClicked(object sender, EventArgs e) => ShowAlertsTab();

    private void ShowResidentsTab()
    {
        SectionResidents.IsVisible = true;
        SectionAlerts.IsVisible = false;

        // Active: Residents (Green)
        BtnTabResidents.BackgroundColor = Color.FromArgb("#047857");
        BtnTabResidents.TextColor = Colors.White;
        BtnTabResidents.BorderColor = Colors.Transparent;

        // Inactive: Alerts (White with Border)
        BtnTabAlerts.BackgroundColor = Colors.White;
        BtnTabAlerts.TextColor = Color.FromArgb("#44403C");
        BtnTabAlerts.BorderColor = Color.FromArgb("#D6D3D1");
    }

    private void ShowAlertsTab()
    {
        SectionResidents.IsVisible = false;
        SectionAlerts.IsVisible = true;

        // Active: Alerts (Rose Red)
        BtnTabAlerts.BackgroundColor = Color.FromArgb("#BE123C");
        BtnTabAlerts.TextColor = Colors.White;
        BtnTabAlerts.BorderColor = Colors.Transparent;

        // Inactive: Residents (White with Border)
        BtnTabResidents.BackgroundColor = Colors.White;
        BtnTabResidents.TextColor = Color.FromArgb("#44403C");
        BtnTabResidents.BorderColor = Color.FromArgb("#D6D3D1");
    }

    // ==========================================
    // ACTIONS: RESIDENT APPROVALS
    // ==========================================
    private async void OnApproveResidentClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is UserModel user)
        {
            bool confirm = await DisplayAlert(
                "Approve Resident",
                $"Confirm verification for {user.FullName} ({user.UnitNo})?",
                "Yes, Approve",
                "Cancel"
            );

            if (confirm)
            {
                await fbService.ApproveResidentAsync(user.FirebaseKey);
                await DisplayAlert("Approved", $"{user.FullName} has been approved as a registered resident.", "OK");
                await LoadAllPendingData();
            }
        }
    }

    private async void OnRejectResidentClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is UserModel user)
        {
            bool confirm = await DisplayAlert(
                "Reject Resident",
                $"Are you sure you want to reject {user.FullName}'s registration?",
                "Reject",
                "Cancel"
            );

            if (confirm)
            {
                await fbService.RejectResidentAsync(user.FirebaseKey);
                await LoadAllPendingData();
            }
        }
    }

    // ==========================================
    // ACTIONS: INCIDENT ALERT APPROVALS
    // ==========================================
    private async void OnApproveAlertClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is AlertModel alert)
        {
            bool confirm = await DisplayAlert(
                "Broadcast Alert",
                $"Broadcast '{alert.Title}' to the community map and feeds?",
                "Broadcast Now 📢",
                "Cancel"
            );

            if (confirm)
            {
                await fbService.ApproveAlertAsync(AppSession.CurrentCommunityCode, alert.FirebaseKey);
                await DisplayAlert("Broadcasted", "Incident alert is now live for all residents.", "OK");
                await LoadAllPendingData();
            }
        }
    }

    private async void OnRejectAlertClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is AlertModel alert)
        {
            bool confirm = await DisplayAlert(
                "Dismiss Alert",
                "Are you sure you want to dismiss this incident report?",
                "Dismiss",
                "Cancel"
            );

            if (confirm)
            {
                await fbService.RejectAlertAsync(AppSession.CurrentCommunityCode, alert.FirebaseKey);
                await LoadAllPendingData();
            }
        }
    }
}