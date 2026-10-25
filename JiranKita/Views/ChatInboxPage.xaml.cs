using JiranKita.Services;

namespace JiranKita.Views;

public partial class ChatInboxPage : ContentPage
{
    public ChatInboxPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LblCommunitySub.Text = AppSession.CurrentCommunityName;
        LblGroupName.Text = AppSession.CurrentCommunityName;
    }

    // Buka Group Chat Komuniti
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

    // Buka Chat 1-on-1 dengan Puan Aisyah
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

    // Buka Chat 1-on-1 dengan Haris
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

    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }
}