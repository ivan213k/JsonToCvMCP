using System.Net.Mail;

namespace JsonToCvApi.Services.Rendering;

public static class Link
{
    /// <summary>Only http(s) absolute URLs become real hrefs — blocks javascript:/data: injection.</summary>
    public static string? CreateHttp(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        return uri.ToString();
    }

    public static string? CreateMailto(string email) =>
        MailAddress.TryCreate(email, out var address) ? $"mailto:{address.Address}" : null;

    public static string? CreateTel(string phone)
    {
        var digits = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
        return digits.Length == 0 ? null : $"tel:{digits}";
    }

    /// <summary>
    /// The human-readable stand-in for a URL — scheme and query stripped, full URL stays in the href.
    /// </summary>
    public static string ToDisplayUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? $"{uri.Host}{uri.AbsolutePath}".TrimEnd('/') : url;
}
