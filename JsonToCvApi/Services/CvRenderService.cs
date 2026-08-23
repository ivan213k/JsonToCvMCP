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

    private static List<object> BuildContactParts(ContactInfo contact)
    {
        var parts = new List<object>
        {
            new { Text = Encode(contact.Email), Href = SafeMailto(contact.Email) },
            new { Text = Encode(contact.Location), Href = (string?)null },
        };

        if (!string.IsNullOrWhiteSpace(contact.Phone))
        {
            parts.Add(new { Text = Encode(contact.Phone), Href = SafeTel(contact.Phone) });
        }

        if (!string.IsNullOrWhiteSpace(contact.LinkedInUrl) && SafeHref(contact.LinkedInUrl) is { } href)
        {
            parts.Add(new { Text = Encode(DisplayUrl(contact.LinkedInUrl)), Href = href });
        }

        return parts;
    }

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
