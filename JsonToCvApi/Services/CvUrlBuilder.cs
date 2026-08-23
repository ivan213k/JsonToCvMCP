namespace JsonToCvApi.Services;

/// <summary>
/// Builds the URL <c>GET /api/cv/{id}</c> serves a rendered PDF from. Shared between the REST
/// handler and the MCP tool so the two entry points can't drift on the URL shape.
/// </summary>
public static class CvUrlBuilder
{
    public static string Build(HttpRequest request, Guid id) => $"{request.Scheme}://{request.Host}/api/cv/{id}";
}
