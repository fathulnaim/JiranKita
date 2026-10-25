namespace JiranKita.Models;

public class AlertModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeDisplay { get; set; } = "Today";
    public bool IsActive { get; set; } = false;
}