namespace JsonToCvApi.Services;

/// <summary>The PDF Info-dictionary fields written onto a rendered CV.</summary>
public sealed record PdfDocumentMetaData(string Title, string Author, string Subject, string Keywords);
