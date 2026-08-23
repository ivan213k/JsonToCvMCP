namespace JsonToCvApi.Services;

public interface IPdfRenderer
{
    /// <summary>
    /// Renders a self-contained HTML document to an A4-paginated PDF.
    /// </summary>
    /// <param name="html">
    /// A complete HTML document (with its own &lt;style&gt;, including any @page rules). The caller is
    /// responsible for producing safe HTML — this renders whatever it's given, unescaped.
    /// </param>
    Task<byte[]> RenderToPdfAsync(string html, CancellationToken cancellationToken = default);
}
