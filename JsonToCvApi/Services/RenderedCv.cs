namespace JsonToCvApi.Services;

/// <summary>A rendered PDF plus the file name <c>GET /api/cv/{id}</c> serves it under.</summary>
public sealed record RenderedCv(byte[] Pdf, string FileName);
