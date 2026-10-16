using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using JiranKita.Models;

namespace JiranKita.Services;

public class FirebaseService
{
    private readonly string _firebaseUrl = "https://jirankita-c5652-default-rtdb.asia-southeast1.firebasedatabase.app/";
    private readonly FirebaseClient _firebaseClient;

    public FirebaseService()
    {
        _firebaseClient = new FirebaseClient(_firebaseUrl);
    }

    // 1. HANTAR MESEJ
    public async Task SendMessageAsync(ChatMessage message)
    {
        await _firebaseClient
            .Child("NeighbourhoodChat")
            .PostAsync(message);
    }

    // 2. PADAM MESEJ DARI FIREBASE
    public async Task DeleteMessageAsync(string firebaseKey)
    {
        if (string.IsNullOrEmpty(firebaseKey)) return;

        await _firebaseClient
            .Child("NeighbourhoodChat")
            .Child(firebaseKey)
            .DeleteAsync();
    }

    // 3. DENGAR MESEJ MASUK DAN MESEJ YANG DIPADAM (LIVE)
    public IDisposable SubscribeToMessages(Action<FirebaseEvent<ChatMessage>> onEventReceived)
    {
        return _firebaseClient
            .Child("NeighbourhoodChat")
            .AsObservable<ChatMessage>()
            .Subscribe(onEventReceived);
    }
}