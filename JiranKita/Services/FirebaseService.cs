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

    // ==========================================
    // 1. USER AUTH (SIGN UP & SIGN IN) -> INI YANG BARU DITAMBAH!
    // ==========================================
    public async Task RegisterUserAsync(UserModel user)
    {
        // Simpan data pendaftaran ke dahan 'Users' di Firebase
        await _firebaseClient
            .Child("Users")
            .PostAsync(user);
    }

    public async Task<UserModel?> LoginUserAsync(string email, string password)
    {
        // Cari pengguna berdasarkan email & password
        var userList = await _firebaseClient
            .Child("Users")
            .OnceAsync<UserModel>();

        foreach (var item in userList)
        {
            if (item.Object.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                item.Object.Password == password)
            {
                var user = item.Object;
                user.FirebaseKey = item.Key;
                return user;
            }
        }
        return null;
    }

    // ==========================================
    // 2. CHAT KOMUNITI (YANG SEDIA ADA)
    // ==========================================
    public async Task SendMessageAsync(ChatMessage message)
    {
        await _firebaseClient
            .Child("NeighbourhoodChat")
            .PostAsync(message);
    }

    public async Task DeleteMessageAsync(string firebaseKey)
    {
        if (string.IsNullOrEmpty(firebaseKey)) return;

        await _firebaseClient
            .Child("NeighbourhoodChat")
            .Child(firebaseKey)
            .DeleteAsync();
    }

    public IDisposable SubscribeToMessages(Action<FirebaseEvent<ChatMessage>> onEventReceived)
    {
        return _firebaseClient
            .Child("NeighbourhoodChat")
            .AsObservable<ChatMessage>()
            .Subscribe(onEventReceived);
    }

    // TAMBAH FUNGSI INI DALAM KELAS FirebaseService.cs:
    public async Task RegisterCommunityAsync(CommunityModel community)
    {
        // Simpan komuniti baharu ke dalam dahan 'Communities'
        await _firebaseClient
            .Child("Communities")
            .PostAsync(community);
    }
}