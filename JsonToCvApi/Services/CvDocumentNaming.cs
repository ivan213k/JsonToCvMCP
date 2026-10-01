using JsonToCvApi.Models;
using PdfSharp.Pdf.IO;

namespace JsonToCvApi.Services;

/// <summary>
/// Names a rendered CV after its owner — both the download file name and the PDF's own document
/// properties. The latter matters more than it looks: browser PDF viewers title the tab from the
/// PDF's <c>/Title</c>, not the file name, so renaming a downloaded file doesn't change what a
/// reader (or an ATS ingesting the metadata) sees.
/// </summary>
public static class CvDocumentNaming
{
    public static string Title(CvData cv) => $"{cv.FullName} – {cv.Headline}";

    /// <summary>"Jane Doe" → "Jane_Doe_CV.pdf". Keeps letters (any script), digits and hyphens only.</summary>
    public static string FileName(CvData cv)
    {
        var parts = cv.FullName
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => new string(part.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray()))
            .Where(part => part.Length > 0)
            .Append("CV");
        return $"{string.Join('_', parts)}.pdf";
    }

    /// <summary>
    /// Fills the PDF's Author/Subject/Keywords. Chromium's PDF export only ever writes <c>/Title</c>
    /// (from the template's <c>&lt;title&gt;</c>) and ignores author/description/keywords meta tags,
    /// so these are set by rewriting the Info dictionary after rendering. Content streams and
    /// embedded fonts are carried over untouched.
    /// </summary>
    public static byte[] ApplyMetadata(byte[] pdf, CvData cv)
    {
        using var input = new MemoryStream(pdf);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        document.Info.Title = Title(cv);
        document.Info.Author = cv.FullName;
        document.Info.Subject = cv.Headline;
        document.Info.Keywords = string.Join(", ", cv.Skills);

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }
}
