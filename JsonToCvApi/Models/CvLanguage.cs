using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToCvApi.Models;

[JsonConverter(typeof(CvLanguageConverter))]
public enum CvLanguage
{
    En,
    De,
    Ua,
    Ru,
    Es,
}

public sealed class CvLanguageConverter()
    : JsonStringEnumConverter<CvLanguage>(namingPolicy: JsonNamingPolicy.CamelCase, allowIntegerValues: false);

public static class CvLanguageParser
{
    /// <summary>
    /// ASP.NET's built-in minimal-API query-string enum binding only matches the exact C# member
    /// name ("De", not "de"), unlike System.Text.Json's case-insensitive enum deserialization (which
    /// the MCP argument path goes through). So the REST handler takes `language` as a plain string
    /// and parses it here instead, keeping both entry points equally tolerant of lowercase codes.
    /// </summary>
    public static CvLanguage Parse(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse<CvLanguage>(value, ignoreCase: true, out var language)
            ? language
            : CvLanguage.En;
}
