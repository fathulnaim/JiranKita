namespace JiranKita.Models;
using Newtonsoft.Json;

public class UserModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Personal & IC
    public string FullName { get; set; } = string.Empty;
    public string IcNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Property & Unit
    public string HousingArea { get; set; } = string.Empty;
    public string Block { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string UnitNo { get; set; } = string.Empty;
    public string OwnershipStatus { get; set; } = string.Empty;

    // Household & Emergency
    public int OccupantsCount { get; set; } = 1;
    public string EmergencyName { get; set; } = string.Empty;
    public string EmergencyRelation { get; set; } = string.Empty;
    public string EmergencyPhone { get; set; } = string.Empty;

    public bool IsVerified { get; set; } = false;
    public string ProofDocument { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasProofDocument => !string.IsNullOrWhiteSpace(ProofDocument);

    [JsonIgnore]
    public ImageSource? ProofImageSource
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ProofDocument))
                return null;

            if (ProofDocument.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return ImageSource.FromUri(new Uri(ProofDocument));

            if (File.Exists(ProofDocument))
                return ImageSource.FromFile(ProofDocument);

            try
            {
                string cleanBase64 = ProofDocument;
                int commaIndex = cleanBase64.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                if (commaIndex >= 0)
                    cleanBase64 = cleanBase64[(commaIndex + 7)..];

                byte[] bytes = Convert.FromBase64String(cleanBase64);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch
            {
                return null;
            }
        }
    }

    public string Role { get; set; } = "Resident";
    public string CommunityCode { get; set; } = string.Empty;
    public string CommunityName { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
}