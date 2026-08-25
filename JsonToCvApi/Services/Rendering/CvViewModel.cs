using System.Globalization;
using JsonToCvApi.Models;

namespace JsonToCvApi.Services.Rendering;

public record CvViewModel(
    string FullName,
    string Headline,
    IReadOnlyList<ContactPart> ContactParts,
    string Summary,
    IReadOnlyList<string> Skills,
    IReadOnlyList<ExperienceViewModel> Experience,
    IReadOnlyList<EducationViewModel> Education,
    IReadOnlyList<CertificationViewModel> Certifications,
    IReadOnlyList<LanguageEntry> Languages)
{
    /// <summary>
    /// Explicit, not <see cref="CultureInfo.CurrentCulture"/>: the document renders in English
    /// whatever locale the container happens to boot with.
    /// </summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    public static CvViewModel From(CvData cv) => new(
        FullName: cv.FullName,
        Headline: cv.Headline,
        ContactParts: ContactLine.Build(cv.Contact),
        Summary: cv.Summary,
        Skills: cv.Skills,
        Experience: cv.Experience.Select(FromExperience).ToList(),
        Education: cv.Education.Select(FromEducation).ToList(),
        // Never null: the template guards with "Certifications.size > 0", and .size throws on null.
        Certifications: (cv.Certifications ?? []).Select(FromCertification).ToList(),
        Languages: cv.Languages);

    private static ExperienceViewModel FromExperience(ExperienceEntry entry) => new(
        Role: entry.Role,
        Company: entry.Company,
        DateRange: MonthRange(entry.StartDate, entry.EndDate),
        ProjectName: entry.ProjectName,
        ProjectDescription: entry.ProjectDescription,
        Highlights: entry.Highlights,
        Technologies: entry.Technologies);

    private static EducationViewModel FromEducation(EducationEntry entry) => new(
        Degree: entry.Degree,
        Institution: entry.Institution,
        Location: entry.Location,
        DateRange: $"{entry.StartDate.Year} – {entry.EndDate.Year}");

    private static CertificationViewModel FromCertification(CertificationEntry entry) => new(
        Name: entry.Name,
        IssuedDisplay: MonthYear(entry.IssuedDate),
        // Null when the URL fails the scheme allow-list — the template guards on it before linking.
        Url: Link.CreateHttp(entry.Url),
        UrlDisplay: entry.Url is null ? null : Link.ToDisplayUrl(entry.Url));

    private static string MonthRange(DateOnly start, DateOnly? end) =>
        $"{MonthYear(start)} – {(end is { } e ? MonthYear(e) : "Present")}";

    private static string MonthYear(DateOnly date) => date.ToString("MMMM yyyy", DisplayCulture);
}

public record ExperienceViewModel(
    string Role,
    string Company,
    string DateRange,
    string? ProjectName,
    string? ProjectDescription,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Technologies);

public record EducationViewModel(
    string Degree,
    string Institution,
    string Location,
    string DateRange);

public record CertificationViewModel(
    string Name,
    string IssuedDisplay,
    string? Url,
    string? UrlDisplay);
