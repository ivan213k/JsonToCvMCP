namespace JsonToCvApi.Models;

/// <summary>
/// One rendered entry in the header contact line, produced by <c>ContactLine.Build</c> from a
/// <see cref="ContactItem"/>. <see cref="Kind"/> picks the icon in the template; <see cref="Href"/>
/// is null for kinds that aren't links, and for values that fail the scheme allow-list.
/// </summary>
public record ContactPart(string Kind, string Text, string? Href);
