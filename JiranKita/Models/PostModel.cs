namespace JiranKita.Models;

public class PostModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Post Details
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, RESOLVED, IN PROGRESS

    // Author Details
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorUnit { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    public string AuthorPhone { get; set; } = string.Empty;
    public bool IsVerifiedAuthor { get; set; } = false;

    // Timestamp & Comment Count
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int CommentsCount { get; set; } = 0;
    public Dictionary<string, CommentModel>? Comments { get; set; }

    // Additional Form Checkbox Options
    public bool AllowDirectContact { get; set; } = true;
    public bool IsUrgentBroadcast { get; set; } = false;

    // Computed Display Logic
    public int DisplayCommentsCount => Comments != null && Comments.Count > 0 ? Comments.Count : CommentsCount;
    public string FormattedDate => CreatedAt.ToString("dd MMM yyyy, h:mm tt");
    public string TimeAgoDisplay => $"📅 {CreatedAt:dd MMM yyyy} · Published";

    // Image Validation (Used by XAML to toggle between actual image vs placeholder)
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    public bool HasNoImage => string.IsNullOrWhiteSpace(ImageUrl);
}