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
    // 1. SEMAK DAN DAFTAR KOMUNITI (BUBBLE)
    // ==========================================
    public async Task<CommunityModel?> FindCommunityByInviteCodeAsync(string inviteCode)
    {
        // Cari terus ke dahan komuniti kod tersebut
        var communityData = await _firebaseClient
            .Child("Communities")
            .Child(inviteCode.ToUpper())
            .Child("Info")
            .OnceSingleAsync<CommunityModel>();

        if (communityData != null)
        {
            communityData.InviteCode = inviteCode.ToUpper();
            return communityData;
        }

        return null;
    }

    public async Task RegisterCommunityAsync(CommunityModel community)
    {
        // Cipta bubble baharu menggunakan InviteCode sebagai nama dahan
        await _firebaseClient
            .Child("Communities")
            .Child(community.InviteCode.ToUpper())
            .Child("Info")
            .PutAsync(community);
    }

    // ==========================================
    // 2. CHAT DALAM BUBBLE (IKUT KOMUNITI SAHAJA)
    // ==========================================
    // 1. HANTAR MESEJ HANYA KE BUBBLE KOMUNITI SPESIFIK
    public async Task SendMessageAsync(string communityCode, ChatMessage message)
    {
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Chat")
            .PostAsync(message);
    }

    // 2. PADAM MESEJ DALAM BUBBLE KOMUNITI SPESIFIK
    public async Task DeleteMessageAsync(string communityCode, string firebaseKey)
    {
        if (string.IsNullOrEmpty(firebaseKey)) return;

        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Chat")
            .Child(firebaseKey)
            .DeleteAsync();
    }

    // 3. DENGAR MESEJ HANYA DARI BUBBLE KOMUNITI SPESIFIK
    public IDisposable SubscribeToMessages(string communityCode, Action<FirebaseEvent<ChatMessage>> onEventReceived)
    {
        return _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Chat")
            .AsObservable<ChatMessage>()
            .Subscribe(onEventReceived);
    }

    // ==========================================
    // 3. FEED POS DALAM BUBBLE (IKUT KOMUNITI SAHAJA)
    // ==========================================
    public async Task CreatePostAsync(string communityCode, PostModel post)
    {
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode)
            .Child("Posts")
            .PostAsync(post);
    }

    public async Task<List<PostModel>> GetAllPostsAsync(string communityCode)
    {
        var postList = await _firebaseClient
            .Child("Communities")
            .Child(communityCode)
            .Child("Posts")
            .OnceAsync<PostModel>();

        var results = new List<PostModel>();
        foreach (var item in postList)
        {
            var post = item.Object;
            post.FirebaseKey = item.Key;
            results.Add(post);
        }

        return results.OrderByDescending(p => p.CreatedAt).ToList();
    }

    // ==========================================
    // 4. KOMEN DALAM BUBBLE
    // ==========================================
    public async Task AddCommentAsync(string communityCode, string postFirebaseKey, CommentModel comment)
    {
        if (string.IsNullOrEmpty(postFirebaseKey)) return;

        await _firebaseClient
            .Child("Communities")
            .Child(communityCode)
            .Child("Posts")
            .Child(postFirebaseKey)
            .Child("Comments")
            .PostAsync(comment);
    }

    public async Task<List<CommentModel>> GetCommentsAsync(string communityCode, string postFirebaseKey)
    {
        if (string.IsNullOrEmpty(postFirebaseKey)) return new List<CommentModel>();

        var list = await _firebaseClient
            .Child("Communities")
            .Child(communityCode)
            .Child("Posts")
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

    // ==========================================
    // 5. AUTH PENGGUNA
    // ==========================================
    public async Task RegisterUserAsync(UserModel user)
    {
        await _firebaseClient
            .Child("Users")
            .PostAsync(user);
    }

    public async Task<UserModel?> LoginUserAsync(string email, string password)
    {
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
}