namespace JsonToCvApi.Models;

public record CvData(
    string FullName,
    string Headline,
    ContactInfo Contact,
    string Summary,
    IReadOnlyList<string> Skills,
    IReadOnlyList<ExperienceEntry> Experience,
    IReadOnlyList<EducationEntry> Education,
    IReadOnlyList<LanguageEntry> Languages,
    IReadOnlyList<CertificationEntry>? Certifications = null);

public record ContactInfo(
    string Email,
    string Location,
    string? Phone = null,
    string? LinkedInUrl = null);

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
