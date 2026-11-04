using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class ChatInboxPage : ContentPage
{
    private readonly FirebaseService fbService = new FirebaseService();
    private List<DirectConversationModel> _directConversations = new();

    public ChatInboxPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LblCommunitySub.Text = AppSession.CurrentCommunityName;
        LblGroupName.Text = AppSession.CurrentCommunityName;

        // Fetch active direct conversation threads from Firebase
        await LoadActiveDirectConversations();
    }

    // Fetches 1-on-1 chat rooms associated with the current user session
    private async Task LoadActiveDirectConversations()
    {
        try
        {
            _directConversations = await fbService.GetDirectChatRoomsAsync(AppSession.CurrentCommunityCode, AppSession.CurrentUserName);
            CvDirectChats.ItemsSource = _directConversations;
        }
        catch
        {
            // Gracefully handles cases when no records exist yet
        }
    }

    // Opens dynamic 1-on-1 private chat room
    private async void OnDirectChatSelected(object sender, SelectionChangedEventArgs e)
    {
        var chat = e.CurrentSelection.FirstOrDefault() as DirectConversationModel;
        if (chat == null) return;

        CvDirectChats.SelectedItem = null;

        var navParams = new Dictionary<string, object>
        {
            { "RoomId", chat.RoomId },
            { "TargetName", chat.NeighbourName },
            { "TargetUnit", chat.NeighbourUnit }
        };

        await Shell.Current.GoToAsync(nameof(Views.ChatRoomPage), navParams);
    }

    // Opens official community group chat room
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

    // Navigates to JiranBot AI Assistant page
    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }
}