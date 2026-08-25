namespace JsonToCvApi.Models;

public record CvData(
    string FullName,
    string Headline,
    IReadOnlyList<ContactItem> Contact,
    string Summary,
    IReadOnlyList<string> Skills,
    IReadOnlyList<ExperienceEntry> Experience,
    IReadOnlyList<EducationEntry> Education,
    IReadOnlyList<LanguageEntry> Languages,
    IReadOnlyList<CertificationEntry>? Certifications = null);

public record ContactItem(string Kind, string Value);

/// <summary>The <see cref="ContactItem.Kind"/> values that get bespoke handling; anything else falls back to a link.</summary>
public static class ContactKinds
{
    public const string Email = "email";
    public const string Address = "address";
    public const string Phone = "phone";
    public const string Link = "link";
    public const string LinkedIn = "linkedin";
    public const string GitHub = "github";
    public const string Social = "social";
}

public record ExperienceEntry(
    string Role,
    string Company,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Technologies,
    string? ProjectName = null,
    string? ProjectDescription = null);

public record EducationEntry(
    string Degree,
    string Institution,
    string Location,
    DateOnly StartDate,
    DateOnly EndDate);

public record CertificationEntry(
    string Name,
    DateOnly IssuedDate,
    string? Url = null);

public record LanguageEntry(
    string Name,
    string Level);
