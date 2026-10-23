namespace JiranKita.Models;

public class PostModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Maklumat Hantaran
    public string Category { get; set; } = "Lost & Found";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = "Block C Lobby";
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, RESOLVED, IN PROGRESS

    // Maklumat Penulis
    public string AuthorName { get; set; } = "Puan Aisyah";
    public string AuthorUnit { get; set; } = "Unit B-12";
    public string AuthorInitials { get; set; } = "PA";
    public bool IsVerifiedAuthor { get; set; } = true;

    // Tarikh & Pengiraan Komen
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int CommentsCount { get; set; } = 0;
    public Dictionary<string, CommentModel>? Comments { get; set; }

    // Pilihan Tambahan dari Checkbox Borang
    public bool AllowDirectContact { get; set; } = true;
    public bool IsUrgentBroadcast { get; set; } = false;

    // Logik Paparan Automatik
    public int DisplayCommentsCount => Comments != null && Comments.Count > 0 ? Comments.Count : CommentsCount;
    public string FormattedDate => CreatedAt.ToString("dd MMM yyyy, h:mm tt");
    public string TimeAgoDisplay => $"📅 {CreatedAt:dd MMM yyyy} · Published";

    // Semakan Gambar (Untuk XAML tahu bila nak tunjuk gambar vs placeholder)
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    public bool HasNoImage => string.IsNullOrWhiteSpace(ImageUrl);
}