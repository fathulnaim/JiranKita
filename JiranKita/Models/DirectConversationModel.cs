namespace JiranKita.Models;

public class DirectConversationModel
{
    public string RoomId { get; set; } = string.Empty;
    public string NeighbourName { get; set; } = string.Empty;
    public string NeighbourUnit { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;

    public string DisplayTitle => string.IsNullOrEmpty(NeighbourUnit)
        ? NeighbourName
        : $"{NeighbourName} ({NeighbourUnit})";

    // Automatically generates 2-letter initials (e.g., "Puan Aisyah" -> "PA")
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(NeighbourName)) return "JK";
            var parts = NeighbourName.Trim().Split(' ');
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            return NeighbourName.Substring(0, Math.Min(2, NeighbourName.Length)).ToUpper();
        }
    }
}