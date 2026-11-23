using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class ApprovalsQueuePage : ContentPage
{
    // Firebase database service instance used to fetch and approve/reject pending residents and alerts
    private readonly FirebaseService fbService = new();

    // Local lists holding the unverified residents and unapproved incident alerts fetched from Firebase
    private List<UserModel> _pendingResidents = new();
    private List<AlertModel> _pendingAlerts = new();

    // Stores which tab ("residents" or "alerts") should be displayed first when the page opens
    private readonly string _initialTab;

    // Default constructor: chains to the parameterized constructor and opens on the "residents" tab by default
    public ApprovalsQueuePage() : this("residents")
    {
    }

    // Overloaded constructor: allows external pages (e.g., Admin Dashboard) to open directly onto "residents" or "alerts"
    public ApprovalsQueuePage(string initialTab)
    {
        InitializeComponent();
        _initialTab = initialTab?.ToLowerInvariant() ?? "residents";
    }

    // Lifecycle hook: runs every time the page appears on screen; sets the active tab and loads fresh DB records
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Apply starting tab selection based on the constructor parameter
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

    // Asynchronously queries Firebase for both pending resident registrations and pending hazard alerts
    private async Task LoadAllPendingData()
    {
        try
        {
            // 1. Fetch pending resident applicants from Firebase and bind them to the Residents CollectionView
            _pendingResidents = await fbService.GetPendingResidentsAsync();
            CvPendingResidents.ItemsSource = _pendingResidents;
            LblResidentsCount.Text = $"Showing {_pendingResidents.Count} pending resident applicant(s)";

            // 2. Fetch pending emergency alert reports for the current community and bind to the Alerts CollectionView
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

    // Event handlers triggered when the admin taps either the Residents or Incident Alerts tab button
    private void OnTabResidentsClicked(object sender, EventArgs e) => ShowResidentsTab();
    private void OnTabAlertsClicked(object sender, EventArgs e) => ShowAlertsTab();

    // Displays the Residents queue section and styles BtnTabResidents as active (Emerald Green)
    private void ShowResidentsTab()
    {
        SectionResidents.IsVisible = true;
        SectionAlerts.IsVisible = false;

        // Active Tab Style: Residents (Emerald Green #047857 background with white text)
        BtnTabResidents.BackgroundColor = Color.FromArgb("#047857");
        BtnTabResidents.TextColor = Colors.White;
        BtnTabResidents.BorderColor = Colors.Transparent;

        // Inactive Tab Style: Alerts (White background with dark gray text and stone border)
        BtnTabAlerts.BackgroundColor = Colors.White;
        BtnTabAlerts.TextColor = Color.FromArgb("#44403C");
        BtnTabAlerts.BorderColor = Color.FromArgb("#D6D3D1");
    }

    // Displays the Incident Alerts queue section and styles BtnTabAlerts as active (Rose Red)
    private void ShowAlertsTab()
    {
        SectionResidents.IsVisible = false;
        SectionAlerts.IsVisible = true;

        // Active Tab Style: Alerts (Crimson/Rose Red #BE123C background with white text)
        BtnTabAlerts.BackgroundColor = Color.FromArgb("#BE123C");
        BtnTabAlerts.TextColor = Colors.White;
        BtnTabAlerts.BorderColor = Colors.Transparent;

        // Inactive Tab Style: Residents (White background with dark gray text and stone border)
        BtnTabResidents.BackgroundColor = Colors.White;
        BtnTabResidents.TextColor = Color.FromArgb("#44403C");
        BtnTabResidents.BorderColor = Color.FromArgb("#D6D3D1");
    }

    // ==========================================
    // ACTIONS: RESIDENT APPROVALS
    // ==========================================

    // Prompts admin confirmation and updates the selected resident's verification status in Firebase
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

    // Prompts admin confirmation and permanently deletes the rejected applicant and their associated community data
    private async void OnRejectResidentClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is UserModel user)
        {
            bool confirm = await DisplayAlert(
                "Reject Resident",
                $"Are you sure you want to reject {user.FullName}'s registration? This will also delete their posts, comments, and chats.",
                "Reject",
                "Cancel"
            );

            if (confirm)
            {
                try
                {
                    string communityCode = !string.IsNullOrWhiteSpace(user.CommunityCode)
                        ? user.CommunityCode
                        : AppSession.CurrentCommunityCode;

                    await fbService.RejectResidentAsync(user.FirebaseKey, communityCode, user.FullName);
                    await LoadAllPendingData();
                }
                catch (Exception error)
                {
                    await DisplayAlert("Error", $"Failed to reject user: {error.Message}", "OK");
                }
            }
        }
    }

    // ==========================================
    // ACTIONS: INCIDENT ALERT APPROVALS
    // ==========================================

    // Prompts admin confirmation and marks the alert as approved in Firebase so it appears on the public map/feed
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

    // Prompts admin confirmation and removes/dismisses the unverified alert report from Firebase
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

    // Opens the fullscreen modal overlay when the admin taps a resident's residency proof document thumbnail
    private void OnProofImageTapped(object sender, TappedEventArgs e)
    {
        UserModel? user = e.Parameter as UserModel
            ?? (sender as BindableObject)?.BindingContext as UserModel;

        if (user?.ProofImageSource == null) return;

        ImgFullscreenProof.Source = user.ProofImageSource;
        ProofOverlay.IsVisible = true;
    }

    // Closes the fullscreen document modal overlay and clears the image source from memory
    private void OnCloseProofOverlayClicked(object sender, EventArgs e)
    {
        ProofOverlay.IsVisible = false;
        ImgFullscreenProof.Source = null;
    }
}