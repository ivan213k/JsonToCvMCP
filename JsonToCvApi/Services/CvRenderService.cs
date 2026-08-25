using System.Globalization;
using System.Net;
using System.Net.Mail;
using JsonToCvApi.Models;
using JsonToCvApi.Templates;
using Scriban;

namespace JsonToCvApi.Services;

public sealed class CvRenderService : ICvRenderService
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly Template ScribanTemplate = ParseTemplate();

    private readonly IPdfRenderer _pdfRenderer;

    public CvRenderService(IPdfRenderer pdfRenderer) => _pdfRenderer = pdfRenderer;

    public async Task<byte[]> RenderToPdfAsync(CvData cv, CancellationToken cancellationToken = default)
    {
        string html = ScribanTemplate.Render(BuildViewModel(cv), member => member.Name);
        return await _pdfRenderer.RenderToPdfAsync(html, cancellationToken);
    }

    private static Template ParseTemplate()
    {
        var template = Template.Parse(SlateTemplate.Shell, "Slate/template.html");
        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"Slate/template.html failed to parse: {string.Join("; ", template.Messages)}");
        }

        return template;
    }

    private static object BuildViewModel(CvData cv) => new
    {
        FullName = Encode(cv.FullName),
        Headline = Encode(cv.Headline),
        ContactParts = BuildContactParts(cv.Contact),
        Summary = Encode(cv.Summary),
        Skills = cv.Skills.Select(Encode).ToList(),
        Experience = cv.Experience.Select(e => new
        {
            Role = Encode(e.Role),
            Company = Encode(e.Company),
            DateRange = FormatMonthRange(e.StartDate, e.EndDate),
            ProjectName = e.ProjectName is null ? null : Encode(e.ProjectName),
            ProjectDescription = e.ProjectDescription is null ? null : Encode(e.ProjectDescription),
            Highlights = e.Highlights.Select(Encode).ToList(),
            Technologies = e.Technologies.Select(Encode).ToList(),
        }).ToList(),
        Education = cv.Education.Select(e => new
        {
            Degree = Encode(e.Degree),
            Institution = Encode(e.Institution),
            Location = Encode(e.Location),
            DateRange = $"{e.StartDate.Year} – {e.EndDate.Year}",
        }).ToList(),
        Certifications = (cv.Certifications ?? []).Select(c => new
        {
            Name = Encode(c.Name),
            IssuedDisplay = c.IssuedDate.ToString("MMMM yyyy", DisplayCulture),
            Url = SafeHref(c.Url),
            UrlDisplay = c.Url is null ? null : Encode(DisplayUrl(c.Url)),
        }).ToList(),
        Languages = cv.Languages.Select(l => new { Name = Encode(l.Name), Level = Encode(l.Level) }).ToList(),
    };

    private static List<object> BuildContactParts(IReadOnlyList<ContactItem> contact) =>
        contact
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .OrderBy(item => item.Kind)
            .Select(BuildContactPart)
            .ToList();

    private static object BuildContactPart(ContactItem item)
    {
        var (text, href) = PartBuilders.GetValueOrDefault(item.Kind, DisplayAsLink)(item.Value);
        return new
        {
            Kind = KindName(item.Kind),
            Text = Encode(string.IsNullOrWhiteSpace(item.Label) ? text : item.Label),
            Href = href,
        };
    }

    private static readonly Dictionary<ContactKind, Func<string, (string Text, string? Href)>> PartBuilders = new()
    {
        [ContactKind.Address] = value => (value, null),
        [ContactKind.Email] = value => (value, SafeMailto(value)),
        [ContactKind.Phone] = value => (value, SafeTel(value)),
        [ContactKind.Link] = DisplayAsLink,
        [ContactKind.Linkedin] = DisplayAsLink,
        [ContactKind.Github] = DisplayAsLink,
        [ContactKind.Social] = DisplayAsLink,
    };

    private static (string Text, string? Href) DisplayAsLink(string value)
    {
        var href = SafeHref(value);
        return (href is null ? value : DisplayUrl(value), href);
    }

    private static string KindName(ContactKind kind) => kind.ToString().ToLowerInvariant();

    private static string FormatMonthRange(DateOnly start, DateOnly? end) =>
        $"{start.ToString("MMMM yyyy", DisplayCulture)} – " +
        $"{(end is { } e ? e.ToString("MMMM yyyy", DisplayCulture) : "Present")}";

    /// <summary>Strips scheme/query for a clean, human-readable link label; the full URL stays in href.</summary>
    private static string DisplayUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? $"{uri.Host}{uri.AbsolutePath}".TrimEnd('/') : url;

    /// <summary>Only http(s) absolute URLs become real hrefs — blocks javascript:/data: injection.</summary>
    private static string? SafeHref(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        return Encode(uri.ToString());
    }

    private static string? SafeMailto(string email) =>
        MailAddress.TryCreate(email, out var address) ? $"mailto:{Encode(address.Address)}" : null;

    private static string? SafeTel(string phone)
    {
        var digits = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
        return digits.Length == 0 ? null : $"tel:{Encode(digits)}";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
