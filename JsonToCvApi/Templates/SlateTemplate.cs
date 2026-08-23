namespace JsonToCvApi.Templates;

public static class SlateTemplate
{
    private const string FontFacePlaceholder = "/*__FONT_FACES__*/";
    private const string TemplateDir = "Slate";

    private static readonly Lazy<string> Cached = new(Build);

    /// <summary>The assembled, self-contained Scriban source, computed once per process.</summary>
    public static string Shell => Cached.Value;

    private static string Build()
    {
        var templateDir = Path.Combine(AppContext.BaseDirectory, "Templates", TemplateDir);
        var html = File.ReadAllText(Path.Combine(templateDir, "template.html"));

        var fontFaces = string.Concat(
            FontFace("Roboto", 400, Path.Combine(templateDir, "Fonts", "roboto-regular.woff2")),
            FontFace("Roboto", 700, Path.Combine(templateDir, "Fonts", "roboto-bold.woff2")),
            FontFace("Libre Bodoni", 400, Path.Combine(templateDir, "Fonts", "librebodoni-regular.woff2")),
            FontFace("Libre Bodoni", 700, Path.Combine(templateDir, "Fonts", "librebodoni-bold.woff2")));

        return html.Replace(FontFacePlaceholder, fontFaces);
    }

    private static string FontFace(string family, int weight, string woff2Path)
    {
        var base64 = Convert.ToBase64String(File.ReadAllBytes(woff2Path));
        return $$"""

            @font-face {
              font-family: '{{family}}';
              font-style: normal;
              font-weight: {{weight}};
              font-display: swap;
              src: url(data:font/woff2;base64,{{base64}}) format('woff2');
            }
            """;
    }
}
