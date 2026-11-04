namespace JiranKita.Models;

public class BotMessageModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MessageText { get; set; } = string.Empty;
    public bool IsUser { get; set; } = false;
    public bool IsBot => !IsUser;

    public string Title { get; set; } = string.Empty;
    public List<string> RulePoints { get; set; } = new();
    public bool HasRulePoints => RulePoints != null && RulePoints.Count > 0;

    // COMBINES ALL BULLET POINTS INTO A CLEAN MULTILINE STRING (PREVENTS NESTED TEMPLATE RENDER ERRORS)
    public string FormattedRulePoints => HasRulePoints
        ? string.Join("\n", RulePoints.Select(p => $"•  {p}"))
        : string.Empty;

    public string SourceCitation { get; set; } = string.Empty;
    public bool HasSource => !string.IsNullOrEmpty(SourceCitation);

    public string ActionButtonText { get; set; } = string.Empty;
    public bool HasActionButton => !string.IsNullOrEmpty(ActionButtonText);

    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string TimeDisplay => Timestamp.ToString("hh:mm tt");
}