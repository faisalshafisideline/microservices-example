using ClubService.Domain.ValueObjects;
using NodaTime;

namespace ClubService.Domain.Entities;

public class Club
{
    public ClubId Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!; // Unique club identifier (e.g., "FC-BARCELONA")
    public string Description { get; private set; } = string.Empty;
    public string Country { get; private set; } = null!;
    public string Currency { get; private set; } = "USD";
    public DateTimeZone Timezone { get; private set; } = DateTimeZone.Utc;
    public string DefaultLanguage { get; private set; } = "en-US";
    public decimal DefaultVatRate { get; private set; } = 0.0m;
    public bool IsActive { get; private set; } = true;
    public ClubType Type { get; private set; } = ClubType.SportsClub;
    public ClubTier Tier { get; private set; } = ClubTier.Basic;
    
    // Contact Information
    public string PrimaryContactEmail { get; private set; } = null!;
    public string PrimaryContactName { get; private set; } = null!;
    public string? PrimaryContactPhone { get; private set; }
    public Address Address { get; private set; } = null!;
    
    // Subscription & Billing
    public SubscriptionPlan SubscriptionPlan { get; private set; } = SubscriptionPlan.Free;
    public LocalDate? SubscriptionExpiresAt { get; private set; }
    public int MaxMembers { get; private set; } = 50; // Based on subscription plan
    public int CurrentMemberCount { get; private set; } = 0;
    
    // Settings
    public ClubSettings Settings { get; private set; } = new();
    
    // Audit
    public Instant CreatedAt { get; private set; }
    public Instant? UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public string? UpdatedBy { get; private set; }

    private Club() { } // EF Core

    public Club(
        string name,
        string code,
        string country,
        string primaryContactEmail,
        string primaryContactName,
        Address address,
        string createdBy,
        string currency = "USD",
        string defaultLanguage = "en-US",
        DateTimeZone? timezone = null)
    {
        Id = ClubId.New();
        Name = name;
        Code = code.ToUpperInvariant();
        Country = country;
        Currency = currency;
        Timezone = timezone ?? DateTimeZone.Utc;
        DefaultLanguage = defaultLanguage;
        PrimaryContactEmail = primaryContactEmail.ToLowerInvariant();
        PrimaryContactName = primaryContactName;
        Address = address;
        CreatedBy = createdBy;
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateBasicInfo(string name, string description, string updatedBy)
    {
        Name = name;
        Description = description;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateContactInfo(string email, string name, string? phone, string updatedBy)
    {
        PrimaryContactEmail = email.ToLowerInvariant();
        PrimaryContactName = name;
        PrimaryContactPhone = phone;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateLocalization(string currency, string language, DateTimeZone timezone, decimal vatRate, string updatedBy)
    {
        Currency = currency;
        DefaultLanguage = language;
        Timezone = timezone;
        DefaultVatRate = vatRate;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateSubscription(SubscriptionPlan plan, LocalDate? expiresAt, int maxMembers, string updatedBy)
    {
        SubscriptionPlan = plan;
        SubscriptionExpiresAt = expiresAt;
        MaxMembers = maxMembers;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateMemberCount(int currentCount)
    {
        CurrentMemberCount = currentCount;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void Activate(string updatedBy)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void Deactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void UpdateSettings(ClubSettings settings, string updatedBy)
    {
        Settings = settings;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public bool CanAddMembers(int additionalMembers = 1)
    {
        return IsActive && (CurrentMemberCount + additionalMembers) <= MaxMembers;
    }

    public bool IsSubscriptionActive()
    {
        if (SubscriptionPlan == SubscriptionPlan.Free)
            return true;

        var today = SystemClock.Instance.GetCurrentInstant().InUtc().Date;
        return SubscriptionExpiresAt == null || SubscriptionExpiresAt > today;
    }
}

public enum ClubType
{
    SportsClub,
    Federation,
    League,
    Academy,
    Community
}

public enum ClubTier
{
    Basic,
    Professional,
    Enterprise
}

public enum SubscriptionPlan
{
    Free,
    Starter,
    Professional,
    Enterprise,
    Custom
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

public class ClubSettings
{
    public bool AllowSelfRegistration { get; set; } = true;
    public bool RequireEmailVerification { get; set; } = true;
    public bool EnableTwoFactorAuth { get; set; } = false;
    public bool AllowGuestAccess { get; set; } = false;
    public int SessionTimeoutMinutes { get; set; } = 60;
    public string? CustomDomain { get; set; }
    public string? LogoUrl { get; set; }
    public string? BrandColor { get; set; } = "#007bff";
    public Dictionary<string, object> CustomFields { get; set; } = new();
} 