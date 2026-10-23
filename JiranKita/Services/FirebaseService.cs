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

    public async Task<CommunityModel?> FindCommunityByInviteCodeAsync(string inviteCode)
    {
        // 1. Tarik semua senarai komuniti dari Firebase
        var communities = await _firebaseClient
            .Child("Communities")
            .OnceAsync<CommunityModel>();

        // 2. Cari komuniti yang mempunyai InviteCode yang sepadan
        foreach (var item in communities)
        {
            if (item.Object.InviteCode.Equals(inviteCode, StringComparison.OrdinalIgnoreCase))
            {
                var community = item.Object;
                community.FirebaseKey = item.Key;
                return community; // Jumpa!
            }
        }

        return null; // Tiada komuniti dengan kod ini
    }

    // --- TAMBAH 2 FUNGSI INI DALAM KELAS FirebaseService.cs ---

    // 1. HANTAR POS BAHARU KE FIREBASE (Dahan 'CommunityPosts')
    public async Task CreatePostAsync(PostModel post)
    {
        await _firebaseClient
            .Child("CommunityPosts")
            .PostAsync(post);
    }

    // 2. TARIK SEMUA SENARAI POS DARI FIREBASE
    public async Task<List<PostModel>> GetAllPostsAsync()
    {
        var postList = await _firebaseClient
            .Child("CommunityPosts")
            .OnceAsync<PostModel>();

        var results = new List<PostModel>();
        foreach (var item in postList)
        {
            var post = item.Object;
            post.FirebaseKey = item.Key;
            results.Add(post);
        }

        // Susun hantaran paling terkini di atas sekali
        return results.OrderByDescending(p => p.CreatedAt).ToList();
    }

    // --- TAMBAH FUNGSI KOMEN INI DALAM KELAS FirebaseService.cs ---

    // 1. HANTAR KOMEN KE POS SPESIFIK
    public async Task AddCommentAsync(string postFirebaseKey, CommentModel comment)
    {
        if (string.IsNullOrEmpty(postFirebaseKey)) return;

        await _firebaseClient
            .Child("CommunityPosts")
            .Child(postFirebaseKey)
            .Child("Comments")
            .PostAsync(comment);
    }

    // 2. TARIK SEMUA KOMEN BAGI POS TERSEBUT
    public async Task<List<CommentModel>> GetCommentsAsync(string postFirebaseKey)
    {
        if (string.IsNullOrEmpty(postFirebaseKey)) return new List<CommentModel>();

        var list = await _firebaseClient
            .Child("CommunityPosts")
            .Child(postFirebaseKey)
            .Child("Comments")
            .OnceAsync<CommentModel>();

        var comments = new List<CommentModel>();
        foreach (var item in list)
        {
            comments.Add(item.Object);
        }
        return comments.OrderBy(c => c.CreatedAt).ToList();
    }
}