namespace JiranKita.Models;

// Data model representing a registered residential community / JMB precinct stored in Firebase ('Communities' node)
public class CommunityModel
{
    // Unique local GUID and the database node key assigned by Firebase Realtime Database
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Property & Community Details (entered by JMB during onboarding in RegisterCommunityPage)
    public string CommunityName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public int TotalUnits { get; set; } = 0;
    public string CobRegNo { get; set; } = string.Empty; // Official COB (Commissioner of Buildings) / ROS registration number
    public string Address { get; set; } = string.Empty;

    // JMB Representative / Chairperson Contact & Verification Proof
    public string RepName { get; set; } = string.Empty;
    public string RepPhone { get; set; } = string.Empty;
    public string CertificateFile { get; set; } = string.Empty;

    // 5-Character Alphanumeric Invite Code (e.g., KAJ82) & generated QR Code URL for resident onboarding
    public string InviteCode { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;

    // System Admin Approval Flag & Creation Timestamp
    public bool IsApproved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}