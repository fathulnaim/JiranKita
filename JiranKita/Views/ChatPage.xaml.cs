using System.Collections.ObjectModel;
using Firebase.Database.Streaming;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class ChatPage : ContentPage
{
    private readonly FirebaseService servisFirebase = new FirebaseService();
    private IDisposable? sambunganLiveChat;
    private CancellationTokenSource? pemasaTekanLama;

    public ObservableCollection<ChatMessage> SenaraiMesej { get; set; } = new ObservableCollection<ChatMessage>();

    public ChatPage()
    {
        InitializeComponent();
        MessagesCollectionView.ItemsSource = SenaraiMesej;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 1. Ambil kod bubble komuniti aktif (cth: KAJ82 atau BGI99)
        string activeCode = AppSession.CurrentCommunityCode;
        LblCommunityTitle.Text = AppSession.CurrentCommunityName;
        LblBubbleCode.Text = $"Bubble: {activeCode} ·";

        // 2. Kosongkan senarai lama supaya mesej komuniti lain tak terselit
        SenaraiMesej.Clear();
        sambunganLiveChat?.Dispose();

        // 3. Dengar mesej HANYA dari bubble komuniti ini
        sambunganLiveChat = servisFirebase.SubscribeToMessages(activeCode, (FirebaseEvent<ChatMessage> peristiwa) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // JIKA MESEJ DIPADAM:
                if (peristiwa.EventType == FirebaseEventType.Delete)
                {
                    var mesejBuang = SenaraiMesej.FirstOrDefault(m => m.FirebaseKey == peristiwa.Key);
                    if (mesejBuang != null)
                    {
                        SenaraiMesej.Remove(mesejBuang);
                        UpdateDateHeaders();
                    }
                }
                // JIKA MESEJ MASUK:
                else if (peristiwa.Object != null)
                {
                    ChatMessage mesejMasuk = peristiwa.Object;
                    mesejMasuk.FirebaseKey = peristiwa.Key;
                    mesejMasuk.IsSelf = (mesejMasuk.SenderName == AppSession.CurrentUserName);

                    // Semak jika mesej belum ada dalam senarai
                    if (!SenaraiMesej.Any(m => m.FirebaseKey == peristiwa.Key))
                    {
                        // SELESAIKAN MISTERI 1: SUSUN MENGIKUT WAKTU (Kronologi Tepat!)
                        InsertMessageSorted(mesejMasuk);

                        MessagesCollectionView.ScrollTo(mesejMasuk, position: ScrollToPosition.End, animate: true);
                    }
                }
            });
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        sambunganLiveChat?.Dispose();
    }

    // ==============================================================
    // FUNGSI PENYELAMAT: Susun mesej supaya soalan sentiasa di atas jawapan!
    // ==============================================================
    private void InsertMessageSorted(ChatMessage msg)
    {
        int index = 0;
        // Cari kedudukan yang betul mengikut waktu
        while (index < SenaraiMesej.Count && SenaraiMesej[index].Timestamp <= msg.Timestamp)
        {
            index++;
        }
        SenaraiMesej.Insert(index, msg);

        UpdateDateHeaders();
    }

    // ==========================================
    // HANTAR MESEJ HANYA KE BUBBLE KOMUNITI AKTIF
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
            Timestamp = DateTime.Now, // Guna UtcNow untuk elak isu beza jam
            IsSelf = true
        };

        try
        {
            // SELESAIKAN MISTERI 2: Hantar ke bubble aktif sahaja!
            await servisFirebase.SendMessageAsync(AppSession.CurrentCommunityCode, newMsg);
        }
        catch (Exception error)
        {
            await DisplayAlert("Ralat", $"Gagal hantar: {error.Message}", "OK");
        }
    }

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
                    await servisFirebase.DeleteMessageAsync(AppSession.CurrentCommunityCode, msg.FirebaseKey);
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

    // ==============================================================
    // LOGIK WHATSAPP: Kira dan paparkan pembahagi tarikh jika hari bertukar
    // ==============================================================
    private void UpdateDateHeaders()
    {
        DateTime? previousDate = null;

        for (int i = 0; i < SenaraiMesej.Count; i++)
        {
            var msg = SenaraiMesej[i];
            var msgDate = msg.Timestamp.ToLocalTime().Date;

            // Jika mesej pertama atau tarikh berbeza daripada mesej sebelumnya:
            if (previousDate == null || msgDate != previousDate.Value)
            {
                msg.ShowDateHeader = true;
                msg.DateHeaderDisplay = GetFriendlyDateString(msgDate);
                previousDate = msgDate;
            }
            else
            {
                msg.ShowDateHeader = false;
            }
        }
    }

    private string GetFriendlyDateString(DateTime date)
    {
        if (date == DateTime.Today)
            return "Today";
        if (date == DateTime.Today.AddDays(-1))
            return "Yesterday";

        // Contoh: "23 October 2026"
        return date.ToString("dd MMMM yyyy");
    }
}