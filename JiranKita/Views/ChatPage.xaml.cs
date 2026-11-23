using System.Collections.ObjectModel;
using Firebase.Database.Streaming;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

// MAUI Shell QueryProperties: receives RoomId, TargetName, and TargetUnit passed during navigation
[QueryProperty(nameof(TargetRoomId), "RoomId")]
[QueryProperty(nameof(TargetUserName), "TargetName")]
[QueryProperty(nameof(TargetUserUnit), "TargetUnit")]
public partial class ChatPage : ContentPage
{
    // Firebase database service instance used for real-time streaming, sending, and deleting messages
    private readonly FirebaseService _firebaseService = new FirebaseService();

    // Holds the active real-time WebSocket/SSE stream subscription so it can be disposed when leaving the page
    private IDisposable? _liveChatSubscription;

    // Cancellation token used to cancel the 500ms press-and-hold timer if the user lifts their finger early
    private CancellationTokenSource? _longPressTimer;

    // Navigation parameters for 1-on-1 direct chat routing (defaults to "Group" for community group chat)
    public string TargetRoomId { get; set; } = "Group";
    public string TargetUserName { get; set; } = string.Empty;
    public string TargetUserUnit { get; set; } = string.Empty;

    // ObservableCollection bound to MessagesCollectionView so incoming/deleted messages update the UI automatically
    public ObservableCollection<ChatMessage> MessageList { get; set; } = new ObservableCollection<ChatMessage>();

    // Constructor: initializes XAML components and binds MessageList to MessagesCollectionView
    public ChatPage()
    {
        InitializeComponent();
        MessagesCollectionView.ItemsSource = MessageList;
    }

    // Lifecycle hook: configures header labels and opens a live Firebase stream (Direct 1-on-1 vs Community Group)
    protected override void OnAppearing()
    {
        base.OnAppearing();

        string communityCode = AppSession.CurrentCommunityCode;
        MessageList.Clear();
        _liveChatSubscription?.Dispose();

        // Check if current session is a 1-on-1 direct message room
        if (!string.IsNullOrEmpty(TargetUserName) && TargetRoomId != "Group")
        {
            LblCommunityTitle.Text = TargetUserName;
            LblBubbleCode.Text = $"{TargetUserUnit} · Direct Chat 🔒";

            // Subscribe to the private 1-on-1 direct message stream in Firebase
            _liveChatSubscription = _firebaseService.SubscribeToDirectMessages(communityCode, TargetRoomId, HandleIncomingMessage);
        }
        // Fallback to precinct public community group chat
        else
        {
            LblCommunityTitle.Text = AppSession.CurrentCommunityName;
            LblBubbleCode.Text = $"Bubble: {communityCode} · Community Group";

            // Subscribe to the public community group message stream in Firebase
            _liveChatSubscription = _firebaseService.SubscribeToMessages(communityCode, HandleIncomingMessage);
        }
    }

    // Real-time Firebase streaming callback: processes incoming new messages and remote deletions on the UI thread
    private void HandleIncomingMessage(FirebaseEvent<ChatMessage> firebaseEvent)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Case 1: A message was deleted from Firebase -> remove it from the UI list and recalculate date badges
            if (firebaseEvent.EventType == FirebaseEventType.Delete)
            {
                var messageToDelete = MessageList.FirstOrDefault(m => m.FirebaseKey == firebaseEvent.Key);
                if (messageToDelete != null)
                {
                    MessageList.Remove(messageToDelete);
                    UpdateDateHeaders();
                }
            }
            // Case 2: A new message arrived -> tag ownership (IsSelf), insert chronologically, and auto-scroll down
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

    // Lifecycle hook: disposes the active Firebase stream listener when leaving the page to prevent memory leaks
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _liveChatSubscription?.Dispose();
    }

    // Chronological insertion sort: inserts each incoming message at the exact index matching its Timestamp
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

    // Dynamic date grouping: compares adjacent message dates to toggle WhatsApp-style date badges ("Today", "Yesterday", or full date)
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

    // Event handler: creates a new ChatMessage and sends it to either the Direct or Group Firebase node
    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        string text = TxtChatMessage.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text)) return;

        // Clear the input field immediately for responsive UX
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
            // Direct message room routing
            if (!string.IsNullOrEmpty(TargetUserName) && TargetRoomId != "Group")
            {
                await _firebaseService.SendDirectMessageAsync(AppSession.CurrentCommunityCode, TargetRoomId, newMsg);
            }
            // Public group room routing
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

    // Pointer gesture handler: starts a 500ms hold timer to show a Copy / Delete for Everyone action sheet
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
                    // Route deletion to either the 1-on-1 room or the community group room in Firebase
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

    // Pointer gesture handler: cancels the 500ms hold timer if the user releases or slides off before 500ms
    private void OnMessagePointerReleased(object sender, PointerEventArgs e)
    {
        _longPressTimer?.Cancel();
    }

    // Navigation handler: opens the JiranBot AI Assistant page
    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }
}