using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

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

public record ContactItem(
    [property: Description(ContactDescriptions.Kind)] ContactKind Kind,
    [property: Description(ContactDescriptions.Value)] string Value,
    [property: Description(ContactDescriptions.Label)] string? Label = null);

[JsonConverter(typeof(ContactKindConverter))]
public enum ContactKind
{
    Address,

    Email,

    Phone,

    Linkedin,

    Github,

    Link,

    Social,
}

public static class ContactDescriptions
{
    public const string Kind = "What this contact entry is. Decides both the icon shown beside it and how the value is linked as well as order.";

    public const string Value = "The contact detail itself.";

    public const string Label = """
        Optional text to display instead of the value — 'Portfolio' in place of 'jane.dev'. Omit it and
        the text is derived from the value: link kinds show the URL with the scheme and any trailing
        slash removed ('linkedin.com/in/jane'), other kinds show the value as given. The link always
        points at the value, never at the label, so a label cannot change where the reader is sent.
        """;
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

public sealed class ContactKindConverter()
    : JsonStringEnumConverter<ContactKind>(namingPolicy: JsonNamingPolicy.CamelCase, allowIntegerValues: false);
