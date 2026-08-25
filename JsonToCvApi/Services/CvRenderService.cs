using JsonToCvApi.Models;
using JsonToCvApi.Services.Rendering;
using JsonToCvApi.Templates;
using Scriban;
using Scriban.Runtime;
namespace JsonToCvApi.Services;

public sealed class CvRenderService : ICvRenderService
{
    private static readonly Template ScribanTemplate = ParseTemplate();
    private static readonly MemberRenamerDelegate MemberRenamer = member => member.Name;

    private readonly IPdfRenderer _pdfRenderer;

    public CvRenderService(IPdfRenderer pdfRenderer) => _pdfRenderer = pdfRenderer;

    public async Task<byte[]> RenderToPdfAsync(CvData cv, CancellationToken cancellationToken = default)
    {
        string html = ScribanTemplate.Render(CreateContext(cv));
        return await _pdfRenderer.RenderToPdfAsync(html, cancellationToken);
    }

    private static Template ParseTemplate()
    {
        var template = Template.Parse(SlateTemplate.Shell, "Slate/template.html");
        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"Slate/template.html failed to parse: {string.Join("; ", template.Messages)}");
        }

        return template;
    }

    /// <summary>
    /// Built by hand rather than via <c>Render(model, renamer)</c>, because that overload constructs
    /// a plain <see cref="TemplateContext"/> and there is no way to substitute the escaping subclass.
    /// </summary>
    private static TemplateContext CreateContext(CvData cv)
    {
        var globals = new ScriptObject();
        globals.Import(CvViewModel.From(cv), renamer: MemberRenamer);

        var context = new HtmlEscapingTemplateContext { MemberRenamer = MemberRenamer };
        context.PushGlobal(globals);
        return context;
    }
}
