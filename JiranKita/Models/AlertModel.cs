namespace JiranKita.Models;

public class AlertModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Category types: Road, Hazards, Security
    public string Category { get; set; } = "Hazards";
    public string Title { get; set; } = string.Empty;

    // Detailed incident or notice description
    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
    public string Distance { get; set; } = "Nearby";
    public string Advisory { get; set; } = string.Empty;
    public string TimeDisplay { get; set; } = "Just now";
    public bool IsActive { get; set; } = true;
    public string Severity { get; set; } = "HIGH";

    // GIS Map Coordinates (Latitude & Longitude)
    public double Latitude { get; set; } = 3.0249;
    public double Longitude { get; set; } = 101.7012;

    // JMB Administrative Approval Status & Reporter Metadata
    public bool IsApproved { get; set; } = false;
    public string ReportedBy { get; set; } = "Resident";
}