namespace JiranKita.Models;

public class CommentModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AuthorName { get; set; } = "Muhammad Fathul";
    public string AuthorUnit { get; set; } = "Unit B-12-04";
    public string CommentText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string TimeAgoDisplay => $"{CreatedAt:dd MMM, h:mm tt}";
}