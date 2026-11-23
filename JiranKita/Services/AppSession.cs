using JiranKita.Models;

namespace JiranKita.Services;

// Global static in-memory session manager storing the active logged-in user and community precinct across all pages
public static class AppSession
{
    // ==============================================================
    // 1. ACTIVE USER IDENTITY & ROLE PROPERTIES
    // ==============================================================

    // Holds the currently signed-in UserModel fetched from Firebase (null when logged out)
    public static UserModel? CurrentUser { get; set; } = null;

    // Quick boolean check verifying if a resident or admin session is active
    public static bool IsLoggedIn => CurrentUser != null;

    // Computed profile properties derived directly from CurrentUser (with safe empty/default fallbacks)
    public static string CurrentUserName => CurrentUser?.FullName ?? string.Empty;
    public static string CurrentUserUnit => CurrentUser?.UnitNo ?? string.Empty;
    public static string CurrentUserPhone => CurrentUser?.Phone ?? string.Empty;
    public static string CurrentUserRole => CurrentUser?.Role ?? "Resident";
    public static bool IsAdmin => CurrentUser?.Role == "Admin";

    // Reads or updates the user's JMB verification status in active session memory
    public static bool IsVerified
    {
        get => CurrentUser?.IsVerified ?? false;
        set
        {
            if (CurrentUser != null)
                CurrentUser.IsVerified = value;
        }
    }

    // Dynamically extracts 2-letter avatar initials from CurrentUser.FullName (e.g., "Ahmad Ali" -> "AA", fallback "JK")
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
    // 2. COMMUNITY BUBBLE CODE (INITIALIZED EMPTY UNTIL JOINED)
    // ==============================================================
    public static string CurrentCommunityCode { get; set; } = string.Empty;
    public static string CurrentCommunityName { get; set; } = string.Empty;

    // ==============================================================
    // 3. SESSION LIFECYCLE CONTROLS (START, SET COMMUNITY & END)
    // ==============================================================

    // Invoked upon successful login or registration to hydrate global session state and restore saved community precinct
    public static void StartSession(UserModel user)
    {
        CurrentUser = user;

        // Restore the user's linked community precinct automatically so they do not re-enter their invite code
        if (!string.IsNullOrWhiteSpace(user.CommunityCode))
        {
            CurrentCommunityCode = user.CommunityCode.Trim().ToUpper();

            // Prefer CommunityName; fall back to HousingArea for legacy accounts
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

    // Checks whether the signed-in user has already been linked to a community invite code
    public static bool HasCommunity => !string.IsNullOrWhiteSpace(CurrentCommunityCode);

    // Links a newly joined community code and name to both AppSession and the active CurrentUser object
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

    // Invoked when tapping 'Log Out'; wipes all in-memory user and community session state
    public static void EndSession()
    {
        CurrentUser = null;
        CurrentCommunityCode = string.Empty;
        CurrentCommunityName = string.Empty;
    }

    // ==============================================================
    // 4. DETERMINISTIC 1-ON-1 CHAT ROOM ID GENERATOR
    // ==============================================================

    // Generates a consistent alphabetical room ID (e.g., "ahmad_siti") so both participants enter the exact same Firebase chat node
    public static string GetDirectRoomId(string userA, string userB)
    {
        string cleanA = userA.Replace(" ", "").ToLower();
        string cleanB = userB.Replace(" ", "").ToLower();

        return string.Compare(cleanA, cleanB) < 0
            ? $"{cleanA}_{cleanB}"
            : $"{cleanB}_{cleanA}";
    }
}