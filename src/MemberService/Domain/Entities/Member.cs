using MemberService.Domain.ValueObjects;
using NodaTime;

namespace MemberService.Domain.Entities;

public class Member
{
    public MemberId Id { get; private set; } = null!;
    public Guid ClubId { get; private set; }
    public Guid UserId { get; private set; } // Reference to AuthService User
    public string MemberNumber { get; private set; } = null!; // Club-specific member number
    
    // Personal Information
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public LocalDate? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }
    public string? Nationality { get; private set; }
    public Address? Address { get; private set; }
    
    // Emergency Contact
    public EmergencyContact? EmergencyContact { get; private set; }
    
    // Membership Details
    public MembershipType MembershipType { get; private set; } = MembershipType.Regular;
    public MembershipStatus Status { get; private set; } = MembershipStatus.Active;
    public LocalDate JoinedDate { get; private set; }
    public LocalDate? ExpiryDate { get; private set; }
    public LocalDate? LastRenewalDate { get; private set; }
    public decimal MembershipFee { get; private set; } = 0.0m;
    public string Currency { get; private set; } = "USD";
    
    // Club Roles & Permissions
    public List<MemberRole> Roles { get; private set; } = new();
    public List<string> Permissions { get; private set; } = new();
    public bool IsClubAdmin { get; private set; } = false;
    
    // Sports-specific
    public List<string> Sports { get; private set; } = new(); // Sports they participate in
    public List<string> Teams { get; private set; } = new(); // Teams they belong to
    public string? Position { get; private set; } // Primary position/role
    public SkillLevel SkillLevel { get; private set; } = SkillLevel.Beginner;
    
    // Medical & Safety
    public List<string> MedicalConditions { get; private set; } = new();
    public List<string> Allergies { get; private set; } = new();
    public string? BloodType { get; private set; }
    public bool HasInsurance { get; private set; } = false;
    public string? InsuranceProvider { get; private set; }
    public LocalDate? LastMedicalCheckup { get; private set; }
    
    // Communication Preferences
    public NotificationPreferences NotificationPreferences { get; private set; } = new();
    
    // Custom Fields
    public Dictionary<string, object> CustomFields { get; private set; } = new();
    
    // Audit
    public Instant CreatedAt { get; private set; }
    public Instant? UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public string? UpdatedBy { get; private set; }

    private Member() { } // EF Core

    public Member(
        Guid clubId,
        Guid userId,
        string memberNumber,
        string firstName,
        string lastName,
        string email,
        MembershipType membershipType,
        decimal membershipFee,
        string currency,
        string createdBy,
        LocalDate? expiryDate = null)
    {
        Id = MemberId.New();
        ClubId = clubId;
        UserId = userId;
        MemberNumber = memberNumber;
        FirstName = firstName;
        LastName = lastName;
        Email = email.ToLowerInvariant();
        MembershipType = membershipType;
        MembershipFee = membershipFee;
        Currency = currency;
        JoinedDate = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
        ExpiryDate = expiryDate;
        CreatedBy = createdBy;
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdatePersonalInfo(
        string firstName,
        string lastName,
        string? phone,
        LocalDate? dateOfBirth,
        Gender? gender,
        string? nationality,
        string updatedBy)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Nationality = nationality;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateAddress(Address address, string updatedBy)
    {
        Address = address;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateEmergencyContact(EmergencyContact emergencyContact, string updatedBy)
    {
        EmergencyContact = emergencyContact;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateMembership(
        MembershipType membershipType,
        decimal membershipFee,
        LocalDate? expiryDate,
        string updatedBy)
    {
        MembershipType = membershipType;
        MembershipFee = membershipFee;
        ExpiryDate = expiryDate;
        LastRenewalDate = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void AddRole(MemberRole role, string updatedBy)
    {
        if (!Roles.Contains(role))
        {
            Roles.Add(role);
            UpdatedBy = updatedBy;
            UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        }
    }

    public void RemoveRole(MemberRole role, string updatedBy)
    {
        if (Roles.Remove(role))
        {
            UpdatedBy = updatedBy;
            UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        }
    }

    public void SetClubAdmin(bool isAdmin, string updatedBy)
    {
        IsClubAdmin = isAdmin;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateSportsInfo(
        List<string> sports,
        List<string> teams,
        string? position,
        SkillLevel skillLevel,
        string updatedBy)
    {
        Sports = sports ?? new List<string>();
        Teams = teams ?? new List<string>();
        Position = position;
        SkillLevel = skillLevel;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateMedicalInfo(
        List<string> medicalConditions,
        List<string> allergies,
        string? bloodType,
        bool hasInsurance,
        string? insuranceProvider,
        LocalDate? lastMedicalCheckup,
        string updatedBy)
    {
        MedicalConditions = medicalConditions ?? new List<string>();
        Allergies = allergies ?? new List<string>();
        BloodType = bloodType;
        HasInsurance = hasInsurance;
        InsuranceProvider = insuranceProvider;
        LastMedicalCheckup = lastMedicalCheckup;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void Activate(string updatedBy)
    {
        Status = MembershipStatus.Active;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void Suspend(string updatedBy)
    {
        Status = MembershipStatus.Suspended;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void Terminate(string updatedBy)
    {
        Status = MembershipStatus.Terminated;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public bool IsActive => Status == MembershipStatus.Active;
    public bool IsMembershipExpired()
    {
        if (ExpiryDate == null) return false;
        var today = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
        return ExpiryDate < today;
    }

    public string FullName => $"{FirstName} {LastName}";
    public int? Age
    {
        get
        {
            if (DateOfBirth == null) return null;
            var today = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
            var age = Period.Between(DateOfBirth.Value, today, PeriodUnits.Years);
            return age.Years;
        }
    }
}

public enum Gender
{
    Male,
    Female,
    Other,
    PreferNotToSay
}

public enum MembershipType
{
    Regular,
    Premium,
    VIP,
    Student,
    Senior,
    Family,
    Corporate,
    Honorary,
    Trial
}

public enum MembershipStatus
{
    Active,
    Inactive,
    Suspended,
    Terminated,
    Pending
}

public enum MemberRole
{
    Member,
    Captain,
    ViceCaptain,
    Coach,
    Trainer,
    Manager,
    Secretary,
    Treasurer,
    Committee,
    Volunteer
}

public enum SkillLevel
{
    Beginner,
    Intermediate,
    Advanced,
    Professional,
    Elite
}

public class Address
{
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string PostalCode { get; private set; } = null!;
    public string Country { get; private set; } = null!;

    private Address() { } // EF Core

    public Address(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }
}

public class EmergencyContact
{
    public string Name { get; private set; } = null!;
    public string Relationship { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string? Email { get; private set; }

    private EmergencyContact() { } // EF Core

    public EmergencyContact(string name, string relationship, string phone, string? email = null)
    {
        Name = name;
        Relationship = relationship;
        Phone = phone;
        Email = email?.ToLowerInvariant();
    }
}

public class NotificationPreferences
{
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool PushNotifications { get; set; } = true;
    public bool EventReminders { get; set; } = true;
    public bool NewsletterSubscription { get; set; } = true;
    public bool MarketingEmails { get; set; } = false;
} 