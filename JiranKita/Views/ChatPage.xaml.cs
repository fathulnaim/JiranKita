using System.Collections.ObjectModel;
using Firebase.Database.Streaming;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

[QueryProperty(nameof(TargetRoomId), "RoomId")]
[QueryProperty(nameof(TargetUserName), "TargetName")]
[QueryProperty(nameof(TargetUserUnit), "TargetUnit")]
public partial class ChatPage : ContentPage
{
    private readonly FirebaseService servisFirebase = new FirebaseService();
    private IDisposable? sambunganLiveChat;
    private CancellationTokenSource? pemasaTekanLama;

    // Parameter jika ini adalah 1-on-1 chat
    public string TargetRoomId { get; set; } = "Group"; // Default: Group Chat
    public string TargetUserName { get; set; } = string.Empty;
    public string TargetUserUnit { get; set; } = string.Empty;

    public ObservableCollection<ChatMessage> SenaraiMesej { get; set; } = new ObservableCollection<ChatMessage>();

    public ChatPage()
    {
        InitializeComponent();
        MessagesCollectionView.ItemsSource = SenaraiMesej;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        string communityCode = AppSession.CurrentCommunityCode;
        SenaraiMesej.Clear();
        sambunganLiveChat?.Dispose();

        // JIKA INI CHAT 1-ON-1 DENGAN JIRAN SPESIFIK
        if (!string.IsNullOrEmpty(TargetUserName) && TargetRoomId != "Group")
        {
            LblCommunityTitle.Text = TargetUserName;
            LblBubbleCode.Text = $"{TargetUserUnit} · Direct Chat 🔒";

            // Dengar mesej dari bilik 1-on-1
            sambunganLiveChat = servisFirebase.SubscribeToDirectMessages(communityCode, TargetRoomId, HandleIncomingMessage);
        }
        // JIKA INI GROUP CHAT KOMUNITI BIASA
        else
        {
            LblCommunityTitle.Text = AppSession.CurrentCommunityName;
            LblBubbleCode.Text = $"Bubble: {communityCode} · Community Group";

            // Dengar mesej dari Group Chat
            sambunganLiveChat = servisFirebase.SubscribeToMessages(communityCode, HandleIncomingMessage);
        }
    }

    private void HandleIncomingMessage(FirebaseEvent<ChatMessage> peristiwa)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (peristiwa.EventType == FirebaseEventType.Delete)
            {
                var mesejBuang = SenaraiMesej.FirstOrDefault(m => m.FirebaseKey == peristiwa.Key);
                if (mesejBuang != null)
                {
                    SenaraiMesej.Remove(mesejBuang);
                    UpdateDateHeaders();
                }
            }
            else if (peristiwa.Object != null)
            {
                ChatMessage mesejMasuk = peristiwa.Object;
                mesejMasuk.FirebaseKey = peristiwa.Key;
                mesejMasuk.IsSelf = (mesejMasuk.SenderName == AppSession.CurrentUserName);

                if (!SenaraiMesej.Any(m => m.FirebaseKey == peristiwa.Key))
                {
                    InsertMessageSorted(mesejMasuk);
                    MessagesCollectionView.ScrollTo(mesejMasuk, position: ScrollToPosition.End, animate: true);
                }
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        sambunganLiveChat?.Dispose();
    }

    private void InsertMessageSorted(ChatMessage msg)
    {
        int index = 0;
        while (index < SenaraiMesej.Count && SenaraiMesej[index].Timestamp <= msg.Timestamp)
        {
            index++;
        }
        SenaraiMesej.Insert(index, msg);
        UpdateDateHeaders();
    }

    private void UpdateDateHeaders()
    {
        DateTime? previousDate = null;
        for (int i = 0; i < SenaraiMesej.Count; i++)
        {
            var msg = SenaraiMesej[i];
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

    // ==========================================
    // HANTAR MESEJ (AUTO DETECT GROUP ATAU 1-ON-1)
    // ==========================================
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
            // Jika 1-on-1: Hantar ke bilik peribadi
            if (!string.IsNullOrEmpty(TargetUserName) && TargetRoomId != "Group")
            {
                await servisFirebase.SendDirectMessageAsync(AppSession.CurrentCommunityCode, TargetRoomId, newMsg);
            }
            // Jika Group: Hantar ke group taman
            else
            {
                await servisFirebase.SendMessageAsync(AppSession.CurrentCommunityCode, newMsg);
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat", $"Gagal hantar: {error.Message}", "OK");
        }
    }

    // PADAM MESEJ (WHATSAPP STYLE)
    private async void OnMessagePointerPressed(object sender, PointerEventArgs e)
    {
        pemasaTekanLama = new CancellationTokenSource();
        Border? border = sender as Border;
        ChatMessage? msg = border?.BindingContext as ChatMessage;
        if (msg == null) return;

        try
        {
            await Task.Delay(500, pemasaTekanLama.Token);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }

                string pilihan = await DisplayActionSheet("Pilihan Mesej", "Batal", "Padam Untuk Semua 🗑️", "Salin Teks 📋");

                if (pilihan == "Salin Teks 📋")
                {
                    await Clipboard.Default.SetTextAsync(msg.MessageText);
                    await DisplayAlert("Disalin", "Teks mesej disalin.", "OK");
                }
                else if (pilihan == "Padam Untuk Semua 🗑️")
                {
                    if (!string.IsNullOrEmpty(TargetUserName) && TargetRoomId != "Group")
                    {
                        await servisFirebase.DeleteDirectMessageAsync(AppSession.CurrentCommunityCode, TargetRoomId, msg.FirebaseKey);
                    }
                    else
                    {
                        await servisFirebase.DeleteMessageAsync(AppSession.CurrentCommunityCode, msg.FirebaseKey);
                    }
                }
            });
        }
        catch (TaskCanceledException) { }
    }

    private void OnMessagePointerReleased(object sender, PointerEventArgs e)
    {
        pemasaTekanLama?.Cancel();
    }

    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }
}