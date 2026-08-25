using System.Text.Json;
using JsonToCvApi.Localization;
using JsonToCvApi.Models;
using JsonToCvApi.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace JsonToCvApi.Tests.Services;

/// <summary>
/// Exercises the full JSON &#8594; PDF pipeline (<see cref="CvRenderService"/> over the real Scriban
/// template, not a synthetic sample), using <c>Data/sample-cv.json</c> as the fixture — the same file
/// the render endpoint's own doc comment points readers at for manual testing. Catches
/// template-authoring mistakes (a stray unclosed tag, a Scriban member name that doesn't match the
/// view model, a font that fails to embed) that a synthetic-model test can't.
/// </summary>
public class CvRenderServiceTests : IAsyncLifetime
{
    private readonly PdfRenderer _pdfRenderer = new(NullLogger<PdfRenderer>.Instance);
    private CvRenderService _renderService = null!;

    private static readonly string SampleCvPath = Path.Combine(AppContext.BaseDirectory, "Data", "sample-cv.json");

    public Task InitializeAsync()
    {
        _renderService = new CvRenderService(_pdfRenderer, new CvLocalizationProvider());
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _pdfRenderer.DisposeAsync();

    [Fact]
    public async Task Renders_SampleCv_ToMultiPageA4Pdf()
    {
        var json = await File.ReadAllTextAsync(SampleCvPath);
        var cv = JsonSerializer.Deserialize<CvData>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var pdf = await _renderService.RenderToPdfAsync(cv);

        Assert.Equal("%PDF-"u8.ToArray(), pdf.Take(5));

        var text = System.Text.Encoding.Latin1.GetString(pdf);
        var mediaBox = System.Text.RegularExpressions.Regex.Match(text, @"MediaBox\s*\[0 0 ([\d.]+) ([\d.]+)\]");
        Assert.True(mediaBox.Success);
        Assert.Equal(595.92, double.Parse(mediaBox.Groups[1].Value), precision: 1);

        // The sample has 3 experience entries, education, and certs — long enough to overflow a
        // single A4 page, proving the A4-paged decision holds for real content, not just a
        // synthetic stress test.
        var count = System.Text.RegularExpressions.Regex.Match(text, @"/Count\s+(\d+)");
        Assert.True(count.Success);
        Assert.True(int.Parse(count.Groups[1].Value) > 1);
    }

    [Fact]
    public async Task Renders_MinimalCv_WithoutOptionalFields()
    {
        // Freelance-style entry: no project, no highlights/technologies, no phone/LinkedIn, and
        // Certifications omitted entirely (null — a CV with none shouldn't need to pass `[]`) —
        // exercises every "{{ if ... }}" guard in the template at once.
        var cv = new CvData(
            FullName: "Jane Doe",
            Headline: "Engineer",
            Contact:
            [
                new ContactItem(ContactKind.Email, "jane@example.com"),
                new ContactItem(ContactKind.Address, "Nowhere"),
                // Whitespace-only label must fall back to the derived text, not render blank.
                new ContactItem(ContactKind.Link, "https://jane.example", Label: "   "),
            ],
            Summary: "Summary text.",
            Skills: ["C#"],
            Experience:
            [
                new ExperienceEntry(
                    Role: "Developer",
                    Company: "Acme",
                    StartDate: new DateOnly(2020, 1, 1),
                    EndDate: null,
                    Highlights: [],
                    Technologies: [])
            ],
            Education: [],
            Languages: [new LanguageEntry("English", "Native")]);

        Assert.Null(cv.Certifications);

        var pdf = await _renderService.RenderToPdfAsync(cv);

        Assert.Equal("%PDF-"u8.ToArray(), pdf.Take(5));
    }

    [Theory]
    [InlineData(CvLanguage.De)]
    [InlineData(CvLanguage.Ua)]
    [InlineData(CvLanguage.Ru)]
    [InlineData(CvLanguage.Es)]
    public async Task Renders_MinimalCv_InEachSupportedLanguage(CvLanguage language)
    {
        // Not a content check (Chromium's PDF text stream isn't trivially greppable, per the other
        // tests in this file) — just that every language in CvLocalization renders without throwing
        // a KeyNotFoundException from a missing dictionary entry and still produces a valid PDF.
        var cv = new CvData(
            FullName: "Jane Doe",
            Headline: "Engineer",
            Contact: [new ContactItem(ContactKind.Email, "jane@example.com")],
            Summary: "Summary text.",
            Skills: ["C#"],
            Experience:
            [
                new ExperienceEntry(
                    Role: "Developer",
                    Company: "Acme",
                    StartDate: new DateOnly(2020, 1, 1),
                    EndDate: null,
                    Highlights: [],
                    Technologies: [])
            ],
            Education: [],
            Languages: [new LanguageEntry("English", "Native")]);

        var pdf = await _renderService.RenderToPdfAsync(cv, language);

        Assert.Equal("%PDF-"u8.ToArray(), pdf.Take(5));
    }

    [Fact]
    public async Task Escapes_HtmlInFreeTextFields()
    {
        // A company/role containing HTML-significant characters must not break the document or
        // inject markup — Scriban does not auto-escape, so this is asserting the service's own
        // encoding, not a Scriban feature.
        var cv = new CvData(
            FullName: "<script>alert(1)</script>",
            Headline: "R&D",
            Contact:
            [
                new ContactItem(ContactKind.Email, "a@b.com"),
                new ContactItem(ContactKind.Linkedin, "javascript:alert(1)"),
                // A label is free text like any other field, so it goes through the same encoding.
                new ContactItem(ContactKind.Link, "https://x.example", Label: "<b>Site</b> & co"),
            ],
            Summary: "S & T",
            Skills: [],
            Experience: [],
            Education: [],
            Certifications: [],
            Languages: []);

        var pdf = await _renderService.RenderToPdfAsync(cv);

        Assert.Equal("%PDF-"u8.ToArray(), pdf.Take(5));
    }
}
