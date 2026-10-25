namespace JiranKita.Models;

public class AlertModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Kategori: Road, Hazards, Security
    public string Category { get; set; } = "Hazards";
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Distance { get; set; } = "Nearby";
    public string Advisory { get; set; } = string.Empty;
    public string TimeDisplay { get; set; } = "Just now";
    public bool IsActive { get; set; } = true;
    public string Severity { get; set; } = "HIGH";

    // KOORDINAT GPS SEBENAR (Default: Kawasan Seri Kembangan)
    public double Latitude { get; set; } = 3.0249;
    public double Longitude { get; set; } = 101.7012;
}