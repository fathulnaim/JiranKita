using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class ChatInboxPage : ContentPage
{
    // Firebase database service instance used to retrieve active 1-on-1 chat rooms
    private readonly FirebaseService fbService = new FirebaseService();

    // Local list caching the resident's private direct conversation threads from Firebase
    private List<DirectConversationModel> _directConversations = new();

    // Constructor: initializes the XAML layout elements defined in ChatInboxPage.xaml
    public ChatInboxPage()
    {
        InitializeComponent();
    }

    // Lifecycle hook: runs every time the user navigates to the inbox; sets community labels and refreshes DMs
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LblCommunitySub.Text = AppSession.CurrentCommunityName;
        LblGroupName.Text = AppSession.CurrentCommunityName;

        // Fetch active direct conversation threads from Firebase for the logged-in resident
        await LoadActiveDirectConversations();
    }

    // Asynchronously fetches 1-on-1 chat rooms associated with the current user session from Firebase
    private async Task LoadActiveDirectConversations()
    {
        try
        {
            _directConversations = await fbService.GetDirectChatRoomsAsync(AppSession.CurrentCommunityCode, AppSession.CurrentUserName);
            CvDirectChats.ItemsSource = _directConversations;
        }
        catch
        {
            // Gracefully handles cases when no direct message records exist yet in Firebase
        }
    }

    // Event handler: triggered when the user taps a 1-on-1 conversation card; passes room parameters and opens ChatRoomPage
    private async void OnDirectChatSelected(object sender, SelectionChangedEventArgs e)
    {
        var chat = e.CurrentSelection.FirstOrDefault() as DirectConversationModel;
        if (chat == null) return;

        // Clear selection immediately so the row does not remain highlighted when returning to the inbox
        CvDirectChats.SelectedItem = null;

        // Bundle the selected room ID and neighbor details to pass to ChatRoomPage
        var navParams = new Dictionary<string, object>
        {
            { "RoomId", chat.RoomId },
            { "TargetName", chat.NeighbourName },
            { "TargetUnit", chat.NeighbourUnit }
        };

        await Shell.Current.GoToAsync(nameof(Views.ChatRoomPage), navParams);
    }

    // Event handler: triggered when the pinned Community Group card is tapped; opens ChatRoomPage in "Group" mode
    private async void OnOpenGroupChatClicked(object sender, EventArgs e)
    {
        var navParams = new Dictionary<string, object>
        {
            { "RoomId", "Group" },
            { "TargetName", AppSession.CurrentCommunityName },
            { "TargetUnit", "Community Group" }
        };

        await Shell.Current.GoToAsync(nameof(Views.ChatRoomPage), navParams);
    }

    // Event handler: triggered when the top-right robot icon is tapped; navigates to the JiranBot AI Assistant page
    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }
}