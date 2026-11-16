using JiranKita.Models;

namespace JiranKita.Services;

public static class AppSession
{

    public static UserModel? CurrentUser { get; set; } = null;

    // Checks whether a user is currently signed in
    public static bool IsLoggedIn => CurrentUser != null;

    // Derived directly from the currently signed-in account details
    public static string CurrentUserName => CurrentUser?.FullName ?? string.Empty;
    public static string CurrentUserUnit => CurrentUser?.UnitNo ?? string.Empty;
    public static string CurrentUserPhone => CurrentUser?.Phone ?? string.Empty;
    public static string CurrentUserRole => CurrentUser?.Role ?? "Resident";
    public static bool IsAdmin => CurrentUser?.Role == "Admin";

    // Dynamically generates initials from the user's real full name
    public static string CurrentUserInitials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CurrentUser?.FullName))
                return "JK";

            var parts = CurrentUser.FullName.Trim().Split(' ');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }

            return CurrentUser.FullName.Substring(0, Math.Min(2, CurrentUser.FullName.Length)).ToUpper();
        }
    }

    // ==============================================================
    // 2. COMMUNITY BUBBLE CODE (INITIALIZED EMPTY
    // ==============================================================
    public static string CurrentCommunityCode { get; set; } = string.Empty;
    public static string CurrentCommunityName { get; set; } = string.Empty;

    // ==============================================================
    // 3. SESSION CONTROLS (START & END)
    // ==============================================================

    // Invoked when a user registers or signs in
    public static void StartSession(UserModel user)
    {
        CurrentUser = user;

        // Restore the saved community so the user is NOT asked for the code again
        if (!string.IsNullOrWhiteSpace(user.CommunityCode))
        {
            CurrentCommunityCode = user.CommunityCode.Trim().ToUpper();

            // Prefer CommunityName; fall back to HousingArea for older accounts
            string name = !string.IsNullOrWhiteSpace(user.CommunityName)
                ? user.CommunityName
                : user.HousingArea;

            CurrentCommunityName = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        }
        else
        {
            CurrentCommunityCode = string.Empty;
            CurrentCommunityName = string.Empty;
        }
    }

    // True once the signed-in user has joined a community
    public static bool HasCommunity => !string.IsNullOrWhiteSpace(CurrentCommunityCode);

    // Updates the session AND the in-memory user in one go (Firebase save is done by the caller)
    public static void SetCommunity(string code, string name)
    {
        CurrentCommunityCode = code.Trim().ToUpper();
        CurrentCommunityName = name.Trim();

        if (CurrentUser != null)
        {
            CurrentUser.CommunityCode = CurrentCommunityCode;
            CurrentUser.CommunityName = CurrentCommunityName;
        }
    }

    // Invoked when the user taps the 'Log Out' button
    public static void EndSession()
    {
        CurrentUser = null;
        CurrentCommunityCode = string.Empty;
        CurrentCommunityName = string.Empty;
    }

    // ==============================================================
    // 4. 1-ON-1 CHAT ROOM ID GENERATOR
    // ==============================================================
    public static string GetDirectRoomId(string userA, string userB)
    {
        string cleanA = userA.Replace(" ", "").ToLower();
        string cleanB = userB.Replace(" ", "").ToLower();

        return string.Compare(cleanA, cleanB) < 0
            ? $"{cleanA}_{cleanB}"
            : $"{cleanB}_{cleanA}";
    }
}