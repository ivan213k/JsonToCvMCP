using System.Net;
using Scriban;
using Scriban.Parsing;

namespace JsonToCvApi.Services.Rendering;

/// <summary>
/// Makes Scriban HTML-escape every interpolated value, the way Razor and Fluid do by default.
/// This is the only place anything is encoded, and it must stay that way: encoding upstream as well
/// yields <c>&amp;amp;</c> in hrefs. <see cref="Link"/>'s builders return raw strings for that reason.
/// </para>
/// </summary>
public sealed class HtmlEscapingTemplateContext : TemplateContext
{
    public override TemplateContext Write(SourceSpan span, object? textAsObject)
    {
        if (textAsObject is null) return this;
        if (textAsObject is RawHtml raw) return Write(raw.Value);

        var text = ObjectToString(textAsObject);
        return text is null ? this : Write(WebUtility.HtmlEncode(text));
    }
}

/// <summary>Opt-out wrapper for a value that is already markup and must not be escaped again.</summary>
public sealed record RawHtml(string Value);
