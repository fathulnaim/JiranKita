namespace JiranKita.Models;

public class AlertModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Category types: Road, Hazards, Security
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    // Detailed incident or notice description
    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
    public string Advisory { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string TimeDisplay { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Severity { get; set; } = string.Empty;

    // Dynamic GIS Map Coordinates (populated from device GPS or active community coordinates)
    public double Latitude { get; set; } = 0.0;
    public double Longitude { get; set; } = 0.0;

    // JMB Administrative Approval Status & Reporter Metadata (populated from AppSession)
    public bool IsApproved { get; set; } = false;
    public string ReportedBy { get; set; } = string.Empty;
    public string ReporterUnit { get; set; } = string.Empty;
}