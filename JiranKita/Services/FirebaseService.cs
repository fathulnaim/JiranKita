using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using JiranKita.Models;

namespace JiranKita.Services;

// Central cloud data-access service wrapping Firebase Realtime Database (Singapore region: asia-southeast1)
public class FirebaseService
{
    private readonly string _firebaseUrl = "https://jirankita-c5652-default-rtdb.asia-southeast1.firebasedatabase.app/";
    private readonly FirebaseClient _firebaseClient;

    // Constructor: initializes the FirebaseClient connection using the regional database endpoint
    public FirebaseService()
    {
        _firebaseClient = new FirebaseClient(_firebaseUrl);
    }

    // ==============================================================
    // 1. COMMUNITY PRECINCT LOOKUP & REGISTRATION ("BUBBLE" ISOLATION)
    // ==============================================================

    // Looks up a community precinct by its 5-character invite code under Communities/{inviteCode}/Info
    public async Task<CommunityModel?> FindCommunityByInviteCodeAsync(string inviteCode)
    {
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

    // Creates a new isolated community branch in Firebase keyed by its uppercase InviteCode
    public async Task RegisterCommunityAsync(CommunityModel community)
    {
        await _firebaseClient
            .Child("Communities")
            .Child(community.InviteCode.ToUpper())
            .Child("Info")
            .PutAsync(community);
    }

    // ==============================================================
    // 2. COMMUNITY GROUP CHAT (REAL-TIME STREAMING & CRUD)
    // ==============================================================

    // Pushes a new group chat message into Communities/{communityCode}/Chat
    public async Task SendMessageAsync(string communityCode, ChatMessage message)
    {
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Chat")
            .PostAsync(message);
    }

    // Deletes a specific group chat message by its Firebase push key
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

    // Opens a live reactive stream (AsObservable) listening for added/removed messages in the community group chat
    public IDisposable SubscribeToMessages(string communityCode, Action<FirebaseEvent<ChatMessage>> onEventReceived)
    {
        return _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Chat")
            .AsObservable<ChatMessage>()
            .Subscribe(onEventReceived);
    }

    // ==============================================================
    // 3. COMMUNITY FEED POSTS (CREATE & FETCH)
    // ==============================================================

    // Saves a new community feed post under Communities/{communityCode}/Posts
    public async Task CreatePostAsync(string communityCode, PostModel post)
    {
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode)
            .Child("Posts")
            .PostAsync(post);
    }

    // Fetches all posts for a community, attaches each item's FirebaseKey, and sorts newest first
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

    // ==============================================================
    // 4. POST COMMENTS
    // ==============================================================

    // Appends a new resident comment under Communities/{communityCode}/Posts/{postFirebaseKey}/Comments
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

    // Retrieves all comments for a specific post sorted chronologically (oldest first)
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

    // ==============================================================
    // 5. USER REGISTRATION & AUTHENTICATION
    // ==============================================================

    // Registers a new UserModel under the global 'Users' node and returns its generated Firebase push key
    public async Task<string> RegisterUserAsync(UserModel user)
    {
        var result = await _firebaseClient
            .Child("Users")
            .PostAsync(user);

        return result.Key;
    }

    // Authenticates a user by matching email (case-insensitive) and password against records in 'Users'
    public async Task<UserModel?> LoginUserAsync(string email, string password)
    {
        var userList = await _firebaseClient
            .Child("Users")
            .OnceAsync<UserModel>();

        foreach (var item in userList)
        {
            if (string.Equals(item.Object.Email?.Trim(), email?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                item.Object.Password == password)
            {
                var user = item.Object;
                user.FirebaseKey = item.Key;
                return user;
            }
        }
        return null;
    }

    // ==============================================================
    // 6. HOME DASHBOARD DATA (URGENT BANNER & NEIGHBOURHOOD PULSE)
    // ==============================================================

    // Fetches the active JMB Urgent Alert banner displayed at the top of HomePage
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

    // Fetches the top N most recent community posts (default 2) for HomePage's Neighbourhood Pulse section
    public async Task<List<PostModel>> GetPulsePostsAsync(string communityCode, int limit = 2)
    {
        var posts = await GetAllPostsAsync(communityCode);
        return posts.Take(limit).ToList();
    }

    // ==============================================================
    // 7. 1-ON-1 DIRECT MESSAGING (WHATSAPP-STYLE PRIVATE ROOMS)
    // ==============================================================

    // Sends a private 1-on-1 message into Communities/{communityCode}/DirectChats/{roomId}/Messages
    public async Task SendDirectMessageAsync(string communityCode, string roomId, ChatMessage message)
    {
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("DirectChats")
            .Child(roomId)
            .Child("Messages")
            .PostAsync(message);
    }

    // Subscribes to live incoming/deleted messages inside a private 1-on-1 chat room
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

    // Deletes a specific message from a 1-on-1 direct chat room
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

    // ==============================================================
    // 8. JMB MODERATION QUEUE (RESIDENT & HAZARD ALERT APPROVALS)
    // ==============================================================

    // Retrieves all active hazard/safety alerts for a community (both approved and pending)
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

                // Include only active alerts
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

    // Fetches non-admin resident accounts awaiting JMB verification (IsVerified == false)
    public async Task<List<UserModel>> GetPendingResidentsAsync()
    {
        var users = await _firebaseClient.Child("Users").OnceAsync<UserModel>();
        var pending = new List<UserModel>();

        foreach (var item in users)
        {
            if (!item.Object.IsVerified && item.Object.Role != "Admin")
            {
                var user = item.Object;
                user.FirebaseKey = item.Key;
                pending.Add(user);
            }
        }
        return pending;
    }

    // Approves a pending resident account by setting IsVerified = true in Firebase
    public async Task ApproveResidentAsync(string userFirebaseKey)
    {
        await _firebaseClient
            .Child("Users")
            .Child(userFirebaseKey)
            .Child("IsVerified")
            .PutAsync(true);
    }

    // Rejects a resident account and performs a cascading delete of their Posts, Comments, Group Chats, Direct Chats, and User record
    public async Task RejectResidentAsync(string userFirebaseKey, string communityCode = "", string userFullName = "")
    {
        if (string.IsNullOrWhiteSpace(userFirebaseKey)) return;

        string cleanCode = communityCode?.Trim().ToUpper() ?? "";
        string targetName = userFullName?.Trim() ?? "";

        if (!string.IsNullOrEmpty(cleanCode) && !string.IsNullOrEmpty(targetName))
        {
            // 1. Delete all Posts and Comments authored by the rejected user
            var posts = await _firebaseClient
                .Child("Communities")
                .Child(cleanCode)
                .Child("Posts")
                .OnceAsync<PostModel>();

            if (posts != null)
            {
                foreach (var post in posts)
                {
                    if (post.Object == null) continue;

                    if (string.Equals(post.Object.AuthorName?.Trim(), targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        await _firebaseClient
                            .Child("Communities")
                            .Child(cleanCode)
                            .Child("Posts")
                            .Child(post.Key)
                            .DeleteAsync();
                    }
                    else if (post.Object.Comments != null)
                    {
                        foreach (var comment in post.Object.Comments)
                        {
                            if (string.Equals(comment.Value?.AuthorName?.Trim(), targetName, StringComparison.OrdinalIgnoreCase))
                            {
                                await _firebaseClient
                                    .Child("Communities")
                                    .Child(cleanCode)
                                    .Child("Posts")
                                    .Child(post.Key)
                                    .Child("Comments")
                                    .Child(comment.Key)
                                    .DeleteAsync();
                            }
                        }
                    }
                }
            }

            // 2. Delete all Group Chat messages sent by the rejected user
            var chatMessages = await _firebaseClient
                .Child("Communities")
                .Child(cleanCode)
                .Child("Chat")
                .OnceAsync<ChatMessage>();

            if (chatMessages != null)
            {
                foreach (var msg in chatMessages)
                {
                    if (string.Equals(msg.Object?.SenderName?.Trim(), targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        await _firebaseClient
                            .Child("Communities")
                            .Child(cleanCode)
                            .Child("Chat")
                            .Child(msg.Key)
                            .DeleteAsync();
                    }
                }
            }

            // 3. Delete all 1-on-1 Direct Chat rooms involving the rejected user
            string cleanUserKey = targetName.Replace(" ", "").ToLower();
            var directRooms = await _firebaseClient
                .Child("Communities")
                .Child(cleanCode)
                .Child("DirectChats")
                .OnceAsync<object>();

            if (directRooms != null)
            {
                foreach (var room in directRooms)
                {
                    var participants = room.Key.Split('_', StringSplitOptions.RemoveEmptyEntries);
                    if (participants.Contains(cleanUserKey, StringComparer.OrdinalIgnoreCase))
                    {
                        await _firebaseClient
                            .Child("Communities")
                            .Child(cleanCode)
                            .Child("DirectChats")
                            .Child(room.Key)
                            .DeleteAsync();
                    }
                }
            }
        }

        // 4. Delete the user's primary profile record from 'Users'
        await _firebaseClient
            .Child("Users")
            .Child(userFirebaseKey)
            .DeleteAsync();
    }

    // Fetches unapproved active hazard alerts awaiting JMB review (IsApproved == false)
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

    // Approves a pending hazard alert so it becomes visible on the live GIS map (IsApproved -> true)
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

    // Rejects/deletes a false or duplicate hazard alert from Firebase
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
    // 9. OFFICIAL JMB NOTICE BROADCAST
    // ==============================================================

    // Publishes an official JMB notice both to UrgentAlert (HomePage banner) and Alerts (pre-approved map pin)
    public async Task SetCommunityUrgentAlertAsync(string communityCode, AlertModel alert)
    {
        // 1. Save to UrgentAlert node for the HomePage yellow warning card
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("UrgentAlert")
            .PutAsync(alert);

        // 2. Also append as an approved active alert so it plots immediately on AlertsPage's GIS map
        alert.IsApproved = true;
        alert.IsActive = true;
        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.ToUpper())
            .Child("Alerts")
            .PostAsync(alert);
    }

    // ==============================================================
    // 10. DIRECT CHAT INBOX & USER PROFILE UPDATES
    // ==============================================================

    // Scans DirectChats rooms matching currentUserName to populate ChatInboxPage with neighbour names and latest messages
    public async Task<List<DirectConversationModel>> GetDirectChatRoomsAsync(string communityCode, string currentUserName)
    {
        var conversationList = new List<DirectConversationModel>();
        string cleanCurrent = currentUserName.Replace(" ", "").ToLower();

        try
        {
            var chatRooms = await _firebaseClient
                .Child("Communities")
                .Child(communityCode.ToUpper())
                .Child("DirectChats")
                .OnceAsync<object>();

            foreach (var room in chatRooms)
            {
                string roomId = room.Key; // e.g., "muhammadfathul_puanaisyah"

                // Include only rooms where the signed-in user is one of the two participants
                if (roomId.Contains(cleanCurrent))
                {
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

                        // Find any message sent by the neighbour to extract their full display name and unit
                        var neighbourMsg = messages.FirstOrDefault(m => m.Object.SenderName != currentUserName)?.Object;

                        string neighbourName = neighbourMsg?.SenderName ?? "Neighbour";
                        string neighbourUnit = neighbourMsg?.SenderUnit ?? "Unit Resident";

                        // Fallback: parse neighbour name from the roomId key if they haven't replied yet
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
            // Ignore if DirectChats node does not exist yet
        }

        return conversationList;
    }

    // Patches a user's linked CommunityCode and CommunityName in Firebase without overwriting other profile fields
    public async Task UpdateUserCommunityAsync(string userFirebaseKey, string communityCode, string communityName)
    {
        if (string.IsNullOrWhiteSpace(userFirebaseKey))
            throw new ArgumentException("Missing user FirebaseKey.");

        string json = System.Text.Json.JsonSerializer.Serialize(new
        {
            CommunityCode = communityCode.Trim().ToUpper(),
            CommunityName = communityName.Trim()
        });

        await _firebaseClient
            .Child("Users")
            .Child(userFirebaseKey)
            .PatchAsync(json);
    }

    // Fetches historical chat messages for a given community and chat room ID
    public async Task<List<ChatMessage>> GetChatMessagesAsync(string communityCode, string roomId)
    {
        if (string.IsNullOrWhiteSpace(communityCode) || string.IsNullOrWhiteSpace(roomId))
            return new List<ChatMessage>();

        var items = await _firebaseClient
            .Child("Communities")
            .Child(communityCode.Trim().ToUpper())
            .Child("Chats")
            .Child(roomId)
            .OnceAsync<ChatMessage>();

        return items
            .Where(x => x.Object != null)
            .Select(x => x.Object)
            .OrderBy(m => m.Timestamp)
            .ToList();
    }

    // ==============================================================
    // 11. RESIDENT HAZARD ALERT SUBMISSION & APPROVED MAP QUERY
    // ==============================================================

    // Submits a new resident hazard alert with IsApproved = false (enters JMB moderation queue first)
    public async Task SubmitResidentAlertAsync(string communityCode, AlertModel alert)
    {
        if (string.IsNullOrWhiteSpace(communityCode))
            throw new ArgumentException("Community code cannot be empty.", nameof(communityCode));

        alert.IsApproved = false;
        alert.IsActive = true;

        await _firebaseClient
            .Child("Communities")
            .Child(communityCode.Trim().ToUpper())
            .Child("Alerts")
            .PostAsync(alert);
    }

    // Returns only active AND JMB-approved alerts for plotting on AlertsPage's GIS map
    public async Task<List<AlertModel>> GetApprovedAlertsAsync(string communityCode)
    {
        var all = await GetCommunityAlertsAsync(communityCode);
        return all.Where(a => a.IsApproved).ToList();
    }
}