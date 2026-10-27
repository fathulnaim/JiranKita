using System.Collections.ObjectModel;
using Firebase.Database.Streaming;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

[QueryProperty(nameof(TargetRoomId), "RoomId")]
[QueryProperty(nameof(TargetUserName), "TargetName")]
[QueryProperty(nameof(TargetUserUnit), "TargetUnit")]
public partial class ChatRoomPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private IDisposable? _liveChatSubscription;
    private CancellationTokenSource? _longPressTimer;

    // Direct chat route parameters (Defaults to "Group")
    public string TargetRoomId { get; set; } = "Group";
    public string TargetUserName { get; set; } = string.Empty;
    public string TargetUserUnit { get; set; } = string.Empty;

    // Observable collection bound to MessagesCollectionView for real-time UI updates
    public ObservableCollection<ChatMessage> MessageList { get; set; } = new ObservableCollection<ChatMessage>();

    // Constructor: Initializes XAML components and binds message collection to CollectionView
    public ChatRoomPage()
    {
        InitializeComponent();
        MessagesCollectionView.ItemsSource = MessageList;
    }

    // Lifecycle hook: Sets up header controls and subscribes to real-time Firebase chat streams (Direct vs Group)
    protected override void OnAppearing()
    {
        base.OnAppearing();

        string communityCode = AppSession.CurrentCommunityCode;
        MessageList.Clear();
        _liveChatSubscription?.Dispose();

        // Check if room is a 1-on-1 direct chat
        if (!string.IsNullOrEmpty(TargetUserName) && TargetRoomId != "Group")
        {
            LblTitle.Text = TargetUserName;
            LblSubtitle.Text = $"{TargetUserUnit} · Direct Chat 🔒";
            _liveChatSubscription = _firebaseService.SubscribeToDirectMessages(communityCode, TargetRoomId, HandleIncomingMessage);
        }
        // Fallback to community group chat
        else
        {
            LblTitle.Text = AppSession.CurrentCommunityName;
            LblSubtitle.Text = $"Bubble: {communityCode} · Community Group";
            _liveChatSubscription = _firebaseService.SubscribeToMessages(communityCode, HandleIncomingMessage);
        }
    }

    // Firebase event handler: Listens for incoming message insertions and deletions in real-time
    private void HandleIncomingMessage(FirebaseEvent<ChatMessage> firebaseEvent)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (firebaseEvent.EventType == FirebaseEventType.Delete)
            {
                var messageToDelete = MessageList.FirstOrDefault(m => m.FirebaseKey == firebaseEvent.Key);
                if (messageToDelete != null)
                {
                    MessageList.Remove(messageToDelete);
                    UpdateDateHeaders();
                }
            }
            else if (firebaseEvent.Object != null)
            {
                ChatMessage incomingMsg = firebaseEvent.Object;
                incomingMsg.FirebaseKey = firebaseEvent.Key;
                incomingMsg.IsSelf = (incomingMsg.SenderName == AppSession.CurrentUserName);

                if (!MessageList.Any(m => m.FirebaseKey == firebaseEvent.Key))
                {
                    InsertMessageSorted(incomingMsg);
                    MessagesCollectionView.ScrollTo(incomingMsg, position: ScrollToPosition.End, animate: true);
                }
            }
        });
    }

    // Lifecycle hook: Disposes live chat subscription when navigating away to prevent memory leaks
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _liveChatSubscription?.Dispose();
    }

    // In-memory sorting: Inserts message chronologically into list based on timestamp
    private void InsertMessageSorted(ChatMessage msg)
    {
        int index = 0;
        while (index < MessageList.Count && MessageList[index].Timestamp <= msg.Timestamp)
        {
            index++;
        }
        MessageList.Insert(index, msg);
        UpdateDateHeaders();
    }

    // Dynamic date grouping: Manages date headers ("Today", "Yesterday", or formatted date string)
    private void UpdateDateHeaders()
    {
        DateTime? previousDate = null;
        for (int i = 0; i < MessageList.Count; i++)
        {
            var msg = MessageList[i];
            var msgDate = msg.Timestamp.ToLocalTime().Date;

            if (previousDate == null || msgDate != previousDate.Value)
            {
                msg.ShowDateHeader = true;
                msg.DateHeaderDisplay = msgDate == DateTime.Today ? "Today" : msgDate == DateTime.Today.AddDays(-1) ? "Yesterday" : msgDate.ToString("dd MMMM yyyy");
                previousDate = msgDate;
            }
            else
            {
                msg.ShowDateHeader = false;
            }
        }
    }

    // Action handler: Transmits new chat message to Firebase (Direct or Group)
    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        string text = TxtChatMessage.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text)) return;

        TxtChatMessage.Text = string.Empty;

        var newMsg = new ChatMessage
        {
            SenderName = AppSession.CurrentUserName,
            SenderUnit = AppSession.CurrentUserUnit,
            MessageText = text,
            Timestamp = DateTime.Now,
            IsSelf = true
        };

        try
        {
            if (!string.IsNullOrEmpty(TargetUserName) && TargetRoomId != "Group")
            {
                await _firebaseService.SendDirectMessageAsync(AppSession.CurrentCommunityCode, TargetRoomId, newMsg);
            }
            else
            {
                await _firebaseService.SendMessageAsync(AppSession.CurrentCommunityCode, newMsg);
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", $"Failed to send message: {error.Message}", "OK");
        }
    }

    // Pointer gesture handler: Implements 500ms long-press action sheet to copy or delete message
    private async void OnMessagePointerPressed(object sender, PointerEventArgs e)
    {
        _longPressTimer = new CancellationTokenSource();
        Border? border = sender as Border;
        ChatMessage? msg = border?.BindingContext as ChatMessage;
        if (msg == null) return;

        try
        {
            await Task.Delay(500, _longPressTimer.Token);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }

                string action = await DisplayActionSheet("Message Options", "Cancel", "Delete for Everyone 🗑️", "Copy Text 📋");

                if (action == "Copy Text 📋")
                {
                    await Clipboard.Default.SetTextAsync(msg.MessageText);
                    await DisplayAlert("Copied", "Message text copied to clipboard.", "OK");
                }
                else if (action == "Delete for Everyone 🗑️")
                {
                    if (!string.IsNullOrEmpty(TargetUserName) && TargetRoomId != "Group")
                    {
                        await _firebaseService.DeleteDirectMessageAsync(AppSession.CurrentCommunityCode, TargetRoomId, msg.FirebaseKey);
                    }
                    else
                    {
                        await _firebaseService.DeleteMessageAsync(AppSession.CurrentCommunityCode, msg.FirebaseKey);
                    }
                }
            });
        }
        catch (TaskCanceledException) { }
    }

    // Pointer gesture handler: Cancels long-press timer if touch is released early
    private void OnMessagePointerReleased(object sender, PointerEventArgs e)
    {
        _longPressTimer?.Cancel();
    }

    // Navigation handler: Opens JiranBot AI Assistant page
    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }
}