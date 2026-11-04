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

    // --- TAMBAH 2 FUNGSI INI DALAM KELAS FirebaseService.cs ---

    // 1. TARIK AMARAN KECEMASAN (URGENT ALERT) IKUT BUBBLE
    public async Task<AlertModel?> GetUrgentAlertAsync(string communityCode)
    {
        try
        {
            return await _firebaseClient
                .Child("Communities")
                .Child(communityCode.ToUpper())
                .Child("UrgentAlert")
                .OnceSingleAsync<AlertModel>();
        }
        catch
        {
            return null;
        }
    }

    // 2. TARIK 2 POS PALING TERKINI UNTUK 'NEIGHBOURHOOD PULSE'
    public async Task<List<PostModel>> GetPulsePostsAsync(string communityCode, int limit = 2)
    {
        var posts = await GetAllPostsAsync(communityCode);
        return posts.Take(limit).ToList();
    }

    // ==========================================
    // FUNGSI 1-ON-1 DIRECT CHAT (WHATSAPP STYLE)
    // ==========================================

    // 1. HANTAR MESEJ 1-ON-1
    public async Task SendDirectMessageAsync(string communityCode, string roomId, ChatMessage message)
    {
        // Simpan mesej ke bilik peribadi di Firebase
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("DirectChats")
            .Child(roomId)
            .Child("Messages")
            .PostAsync(message);
    }

    // 2. DENGAR MESEJ 1-ON-1 SECARA LIVE
    public IDisposable SubscribeToDirectMessages(string communityCode, string roomId, Action<FirebaseEvent<ChatMessage>> onEventReceived)
    {
        return _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("DirectChats")
            .Child(roomId)
            .Child("Messages")
            .AsObservable<ChatMessage>()
            .Subscribe(onEventReceived);
    }

    // 3. PADAM MESEJ 1-ON-1
    public async Task DeleteDirectMessageAsync(string communityCode, string roomId, string firebaseKey)
    {
        if (string.IsNullOrEmpty(firebaseKey)) return;

        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("DirectChats")
            .Child(roomId)
            .Child("Messages")
            .Child(firebaseKey)
            .DeleteAsync();
    }

    // ==========================================
    // TARIK AMARAN BENCANA/KESELAMATAN DARI BUBBLE
    // ==========================================
    public async Task<List<AlertModel>> GetCommunityAlertsAsync(string communityCode)
    {
        try
        {
            var alertList = await _firebaseClient
                .Child("Communities")
                .Child(communityCode.ToUpper())
                .Child("Alerts")
                .OnceAsync<AlertModel>();

            var results = new List<AlertModel>();
            foreach (var item in alertList)
            {
                var alert = item.Object;
                alert.FirebaseKey = item.Key;

                // Hanya ambil amaran yang aktif
                if (alert.IsActive)
                {
                    results.Add(alert);
                }
            }
            return results;
        }
        catch
        {
            return new List<AlertModel>();
        }
    }

    // 1. Tarik senarai penduduk yang belum disahkan (IsVerified == false)
    public async Task<List<UserModel>> GetPendingResidentsAsync()
    {
        var users = await _firebaseClient.Child("Users").OnceAsync<UserModel>();
        var pending = new List<UserModel>();

        foreach (var item in users)
        {
            if (!item.Object.IsVerified )
            //tukar kat sini && item.Object.Role != "Admin"
            {
                var user = item.Object;
                user.FirebaseKey = item.Key;
                pending.Add(user);
            }
        }
        return pending;
    }

    // 2. Luluskan Penduduk (Tukar IsVerified -> true)
    public async Task ApproveResidentAsync(string userFirebaseKey)
    {
        await _firebaseClient
            .Child("Users")
            .Child(userFirebaseKey)
            .Child("IsVerified")
            .PutAsync(true);
    }

    // 3. Tolak Penduduk
    public async Task RejectResidentAsync(string userFirebaseKey)
    {
        await _firebaseClient
            .Child("Users")
            .Child(userFirebaseKey)
            .DeleteAsync();
    }

    // 4. Tarik senarai amaran bahaya yang menunggu kelulusan JMB (IsApproved == false)
    public async Task<List<AlertModel>> GetPendingAlertsAsync(string communityCode)
    {
        var alerts = await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Alerts")
            .OnceAsync<AlertModel>();

        var pending = new List<AlertModel>();
        foreach (var item in alerts)
        {
            if (!item.Object.IsApproved && item.Object.IsActive)
            {
                var alert = item.Object;
                alert.FirebaseKey = item.Key;
                pending.Add(alert);
            }
        }
        return pending;
    }

    // 5. Luluskan & Siarkan Amaran ke Peta (Tukar IsApproved -> true)
    public async Task ApproveAlertAsync(string communityCode, string alertFirebaseKey)
    {
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Alerts")
            .Child(alertFirebaseKey)
            .Child("IsApproved")
            .PutAsync(true);
    }

    // 6. Buang Amaran Palsu / Tolak Amaran
    public async Task RejectAlertAsync(string communityCode, string alertFirebaseKey)
    {
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Alerts")
            .Child(alertFirebaseKey)
            .DeleteAsync();
    }

    // ==============================================================
    // FUNGSI SIARKAN NOTIS RASMI JMB KE FIREBASE
    // ==============================================================
    public async Task SetCommunityUrgentAlertAsync(string communityCode, AlertModel alert)
    {
        // 1. Simpan ke dahan UrgentAlert (Untuk Kad Kuning di HomePage)
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("UrgentAlert")
            .PutAsync(alert);

        // 2. Simpan juga ke senarai Alerts (Untuk Peta GIS di AlertsPage)
        alert.IsApproved = true;
        alert.IsActive = true;
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Alerts")
            .PostAsync(alert);
    }

    // ==============================================================
    // TARIK SENARAI SEMBANG 1-ON-1 YANG AKTIF DARI DATABASE
    // ==============================================================
    public async Task<List<DirectConversationModel>> GetDirectChatRoomsAsync(string communityCode, string currentUserName)
    {
        var conversationList = new List<DirectConversationModel>();
        string cleanCurrent = currentUserName.Replace(" ", "").ToLower();

        try
        {
            // Ambil semua bilik DirectChats dalam bubble komuniti
            var chatRooms = await _firebaseClient
                .Child("Communities")
                .Child(communityCode.ToUpper())
                .Child("DirectChats")
                .OnceAsync<object>();

            foreach (var room in chatRooms)
            {
                string roomId = room.Key; // cth: "muhammadfathul_puanaisyah"

                // Hanya ambil bilik jika pengguna terlibat dalam perbualan itu
                if (roomId.Contains(cleanCurrent))
                {
                    // Tarik mesej dalam bilik ini untuk dapatkan mesej terakhir & profil jiran
                    var messages = await _firebaseClient
                        .Child("Communities")
                        .Child(communityCode.ToUpper())
                        .Child("DirectChats")
                        .Child(roomId)
                        .Child("Messages")
                        .OnceAsync<ChatMessage>();

                    if (messages != null && messages.Any())
                    {
                        var sortedMessages = messages.OrderBy(m => m.Object.Timestamp).ToList();
                        var lastMsg = sortedMessages.Last().Object;

                        // Cari mesej jiran untuk dapatkan nama & unitnya
                        var neighbourMsg = messages.FirstOrDefault(m => m.Object.SenderName != currentUserName)?.Object;

                        string neighbourName = neighbourMsg?.SenderName ?? "Neighbour";
                        string neighbourUnit = neighbourMsg?.SenderUnit ?? "Unit Resident";

                        // Jika kita baru hantar mesej dan jiran belum balas, ekstrak nama dari RoomId
                        if (neighbourName == "Neighbour")
                        {
                            string rawName = roomId.Replace(cleanCurrent, "").Replace("_", "");
                            if (!string.IsNullOrEmpty(rawName))
                            {
                                neighbourName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName);
                            }
                        }

                        conversationList.Add(new DirectConversationModel
                        {
                            RoomId = roomId,
                            NeighbourName = neighbourName,
                            NeighbourUnit = neighbourUnit,
                            LastMessage = lastMsg.MessageText
                        });
                    }
                }
            }
        }
        catch
        {
            // Jika belum ada rekod
        }

        return conversationList;
    }
}