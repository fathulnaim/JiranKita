namespace JiranKita.Models;

// Data model representing a single resident comment attached to a community post in Firebase
public class CommentModel
{
    // Unique local GUID assigned when instantiating a new comment object
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Generic fallback values (overwritten dynamically with AppSession.CurrentUserName & CurrentUserUnit at runtime)
    public string AuthorName { get; set; } = "Guest";
    public string AuthorUnit { get; set; } = string.Empty;

    // The comment body text and creation timestamp stored in Firebase
    public string CommentText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Computed read-only property formatted for UI display in PostDetailPage.xaml (e.g., "24 Nov, 7:45 AM")
    public string TimeAgoDisplay => $"{CreatedAt:dd MMM, h:mm tt}";
}