namespace JiranKita.Models;

public class CommunityModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Maklumat Hartanah / Komuniti
    public string CommunityName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public int TotalUnits { get; set; } = 0;
    public string CobRegNo { get; set; } = string.Empty; // No Pendaftaran COB/ROS
    public string Address { get; set; } = string.Empty;

    // Maklumat Wakil JMB / Pengerusi
    public string RepName { get; set; } = string.Empty;
    public string RepPhone { get; set; } = string.Empty;
    public string CertificateFile { get; set; } = string.Empty;

    // Kod Jemputan 5-Digit (cth: KAJ82 untuk penduduk scan/taip)
    public string InviteCode { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;

    // Status Kelulusan Admin JiranKita
    public bool IsApproved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}