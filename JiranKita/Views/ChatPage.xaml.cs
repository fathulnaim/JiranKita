using System.Collections.ObjectModel;
using Firebase.Database.Streaming;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class ChatPage : ContentPage
{
    private readonly FirebaseService servisFirebase = new FirebaseService();
    private IDisposable? sambunganLiveChat;

    // Pemasa untuk mengesan berapa lama jari ditekan pada mesej
    private CancellationTokenSource? pemasaTekanLama;

    public ObservableCollection<ChatMessage> SenaraiMesej { get; set; } = new ObservableCollection<ChatMessage>();

    private readonly string namaSaya = "Muhammad Fathul";
    private readonly string unitSaya = "Unit B-12-04";

    public ChatPage()
    {
        InitializeComponent();
        MessagesCollectionView.ItemsSource = SenaraiMesej;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // DENGAR PERISTIWA DARI FIREBASE (Mesej Baru atau Mesej Dipadam)
        sambunganLiveChat = servisFirebase.SubscribeToMessages((FirebaseEvent<ChatMessage> peristiwa) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // JIKA MESEJ DIPADAM DI FIREBASE: Padamkan dari skrin telefon serta-merta
                if (peristiwa.EventType == FirebaseEventType.Delete)
                {
                    ChatMessage? mesejUntukDibuang = null;
                    foreach (ChatMessage item in SenaraiMesej)
                    {
                        if (item.FirebaseKey == peristiwa.Key)
                        {
                            mesejUntukDibuang = item;
                            break;
                        }
                    }

                    if (mesejUntukDibuang != null)
                    {
                        SenaraiMesej.Remove(mesejUntukDibuang);
                    }
                }
                // JIKA MESEJ BARU MASUK:
                else if (peristiwa.Object != null)
                {
                    ChatMessage mesejMasuk = peristiwa.Object;
                    mesejMasuk.FirebaseKey = peristiwa.Key; // Simpan kunci Firebase

                    bool sudahAda = false;
                    foreach (ChatMessage m in SenaraiMesej)
                    {
                        if (m.FirebaseKey == peristiwa.Key)
                        {
                            sudahAda = true;
                            break;
                        }
                    }

                    if (!sudahAda)
                    {
                        mesejMasuk.IsSelf = (mesejMasuk.SenderName == namaSaya);
                        SenaraiMesej.Add(mesejMasuk);
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

    // ==========================================
    // 1. BILA JARI MULA TEKAN MESEJ (MULA KIRA MASA)
    // ==========================================
    private async void OnMessagePointerPressed(object sender, PointerEventArgs e)
    {
        pemasaTekanLama = new CancellationTokenSource();
        Border? kotakMesej = sender as Border;
        ChatMessage? mesejPilihan = kotakMesej?.BindingContext as ChatMessage;

        if (mesejPilihan == null) return;

        try
        {
            // Tunggu 500ms (0.5 saat) untuk sahkan sebagai "Tekan Lama"
            await Task.Delay(500, pemasaTekanLama.Token);

            // JIKA BERJAYA TAHAN 0.5 SAAT:
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // A. Getarkan telefon sikit (Haptic Feedback)
                try
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                }
                catch { /* sesetengah peranti/emulator tiada vibration motor */ }

                // B. Buka menu pilihan WhatsApp dari bawah
                string pilihan = await DisplayActionSheet(
                    "Pilihan Mesej",
                    "Batal",
                    "Padam Untuk Semua 🗑️",
                    "Salin Teks 📋"
                );

                // Jika pilih Salin
                if (pilihan == "Salin Teks 📋")
                {
                    await Clipboard.Default.SetTextAsync(mesejPilihan.MessageText);
                    await DisplayAlert("Disalin", "Teks mesej telah disalin.", "OK");
                }
                // Jika pilih Padam
                else if (pilihan == "Padam Untuk Semua 🗑️")
                {
                    try
                    {
                        await servisFirebase.DeleteMessageAsync(mesejPilihan.FirebaseKey);
                    }
                    catch (Exception ralat)
                    {
                        await DisplayAlert("Ralat", $"Gagal memadam: {ralat.Message}", "OK");
                    }
                }
            });
        }
        catch (TaskCanceledException)
        {
            // Jari diangkat sebelum sempat 0.5 saat (tekan biasa)
        }
    }

    // ==========================================
    // 2. BILA JARI DIANGKAT (BATALKAN JIKA TEKAN SEKEJAP)
    // ==========================================
    private void OnMessagePointerReleased(object sender, PointerEventArgs e)
    {
        pemasaTekanLama?.Cancel();
    }

    // ==========================================
    // 3. BUTANG HANTAR MESEJ (➤)
    // ==========================================
    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        string teksDitaip = TxtChatMessage.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(teksDitaip)) return;

        TxtChatMessage.Text = string.Empty;

        ChatMessage mesejBaru = new ChatMessage
        {
            SenderName = namaSaya,
            SenderUnit = unitSaya,
            MessageText = teksDitaip,
            Timestamp = DateTime.Now,
            IsSelf = true
        };

        try
        {
            await servisFirebase.SendMessageAsync(mesejBaru);
        }
        catch (Exception ralat)
        {
            await DisplayAlert("Ralat", $"Mesej gagal dihantar: {ralat.Message}", "OK");
        }
    }

    // Buka skrin JiranBot bila tekan robot 🤖
    private async void OnRobotClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Views.JiranBotPage));
    }
}