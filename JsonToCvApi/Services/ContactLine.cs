using JsonToCvApi.Models;
using JsonToCvApi.Services.Rendering;

namespace JsonToCvApi.Services;

public static class ContactLine
{
    private static readonly Dictionary<string, int> KindOrder = new()
    {
        [ContactKinds.Address] = 0,
        [ContactKinds.Email] = 1,
        [ContactKinds.Phone] = 2,
        [ContactKinds.LinkedIn] = 3,
    };

    private const int Unranked = int.MaxValue;

    public static IReadOnlyList<ContactPart> Build(IReadOnlyList<ContactItem> contact) => contact
        .Where(item => !string.IsNullOrWhiteSpace(item.Value))
        .Select(item => BuildPart(NormalizeKind(item.Kind), item.Value))
        .OrderBy(part => KindOrder.GetValueOrDefault(part.Kind, Unranked))
        .ToList();

    private static ContactPart BuildPart(string kind, string value) =>
        new(kind, DisplayText(kind, value), Href(kind, value));

    private static string NormalizeKind(string kind) => kind.Trim().ToLowerInvariant();

    private static string DisplayText(string kind, string value)
    {
        if (kind == ContactKinds.Email || kind == ContactKinds.Address || kind == ContactKinds.Phone)
        {
            return value;
        }

        // Link-ish kinds show a trimmed URL; a value that isn't a URL passes through untouched.
        return Link.ToDisplayUrl(value);
    }

    private static string? Href(string kind, string value)
    {
        if (kind == ContactKinds.Email)
        {
            return Link.CreateMailto(value);
        }

        if (kind == ContactKinds.Phone)
        {
            return Link.CreateTel(value);
        }

        if (kind == ContactKinds.Address)
        {
            return null;
        }

        // "link" and anything the caller invents: a real href only if the value parses as http(s).
        return Link.CreateHttp(value);
    }
}
