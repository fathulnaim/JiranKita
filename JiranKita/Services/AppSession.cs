using JiranKita.Models;

namespace JiranKita.Services;

public static class AppSession
{
    // 1. KOD BUBBLE AKTIF (Default "KAJ82" supaya masa testing tak payah login pun app jalan!)
    public static string CurrentCommunityCode { get; set; } = "KAJ82";
    public static string CurrentCommunityName { get; set; } = "Taman Seri Kembangan (Zone B)";

    // 2. PROFIL PENGGUNA AKTIF
    public static string CurrentUserName { get; set; } = "Muhammad Fathul";
    public static string CurrentUserUnit { get; set; } = "Unit B-12-04";
    public static string CurrentUserInitials { get; set; } = "FN";

    // ==========================================
    // FORMULA JANA ID BILIK 1-ON-1 WHATSAPP STYLE
    // ==========================================
    public static string GetDirectRoomId(string userA, string userB)
    {
        string cleanA = userA.Replace(" ", "").ToLower();
        string cleanB = userB.Replace(" ", "").ToLower();

        // Susun ikut abjad supaya kedua-dua user sentiasa dapat ID bilik yang sama
        return string.Compare(cleanA, cleanB) < 0
            ? $"{cleanA}_{cleanB}"
            : $"{cleanB}_{cleanA}";
    }
}
