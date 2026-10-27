using JiranKita.Services;

namespace JiranKita.Views;

public partial class ChatInboxPage : ContentPage
{
    // Constructor: Initializes XAML layout components
    public ChatInboxPage()
    {
        InitializeComponent();
    }

    // Lifecycle hook: Automatically updates header and community group labels with active session data when page appears
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LblCommunitySub.Text = AppSession.CurrentCommunityName;
        LblGroupName.Text = AppSession.CurrentCommunityName;
    }

    // Navigates to the public Community Group Chat room via Shell parameter routing
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

    // Opens a 1-on-1 direct message room with resident Puan Aisyah using a deterministic room ID
    private async void OnOpenAisyahChatClicked(object sender, EventArgs e)
    {
        string roomId = AppSession.GetDirectRoomId(AppSession.CurrentUserName, "Puan Aisyah");
        var navParams = new Dictionary<string, object>
        {
            { "RoomId", roomId },
            { "TargetName", "Puan Aisyah" },
            { "TargetUnit", "Unit B-12" }
        };
        await Shell.Current.GoToAsync(nameof(Views.ChatRoomPage), navParams);
    }

    // Opens a 1-on-1 direct message room with resident Haris using a deterministic room ID
    private async void OnOpenHarisChatClicked(object sender, EventArgs e)
    {
        string roomId = AppSession.GetDirectRoomId(AppSession.CurrentUserName, "Haris");
        var navParams = new Dictionary<string, object>
        {
            { "RoomId", roomId },
            { "TargetName", "Haris" },
            { "TargetUnit", "Unit B-08" }
        };
        await Shell.Current.GoToAsync(nameof(Views.ChatRoomPage), navParams);
    }

    // Navigates to the JiranBot AI Assistant page
    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }
}