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
        var kind = KindName(item.Kind);
        var label = string.IsNullOrWhiteSpace(item.Label) ? null : Encode(item.Label);

        if (item.Kind == ContactKind.Email) return new { Kind = kind, Text = label ?? Encode(item.Value), Href = SafeMailto(item.Value) };
        if (item.Kind == ContactKind.Phone) return new { Kind = kind, Text = label ?? Encode(item.Value), Href = SafeTel(item.Value) };
        if (item.Kind == ContactKind.Address) return new { Kind = kind, Text = label ?? Encode(item.Value), Href = (string?)null };

        // Only shorten to a link label when it really is a link; anything else shows verbatim.
        var href = SafeHref(item.Value);
        return new { Kind = kind, Text = label ?? Encode(href is null ? item.Value : DisplayUrl(item.Value)), Href = href };
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
