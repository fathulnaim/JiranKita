namespace JiranKita.Models;

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
}