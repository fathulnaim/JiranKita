namespace JiranKita.Models;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderUnit { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    // Untuk tentukan sama ada mesej kita sendiri (hijau di kanan) atau jiran (putih di kiri)
    public bool IsSelf { get; set; }
}