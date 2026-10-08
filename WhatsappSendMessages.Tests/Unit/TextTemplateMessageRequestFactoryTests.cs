using WhatsappBusiness.CloudApi;
using WhatsappSendMessages.Entities.Request;
using WhatsappSendMessages.Services.Templates;

namespace WhatsappSendMessages.Tests.Unit;

public class TextTemplateMessageRequestFactoryTests
{
    private readonly TextTemplateMessageRequestFactory _sut = new();

    [Fact]
    public void SinParametros_NoIncluyeComponente()
    {
        var req = new TemplateRequest { Number = "5215512345678", TemplateName = "t" };

        var result = _sut.Create(req);

        Assert.Equal("5215512345678", result.To);
        Assert.Equal("t", result.Template.Name);
        Assert.Equal(LanguageCode.Spanish_MEX, result.Template.Language.Code);
        Assert.Null(result.Template.Components);
    }

    [Fact]
    public void ConParametros_GeneraComponenteBodyConCadaParametro()
    {
        var req = new TemplateRequest
        {
            Number = "5215512345678",
            TemplateName = "saludo",
            Parameters = new List<TemplateParameter>
            {
                new() { Type = "text", ParameterName = "nombre", Text = "Juan" },
                new() { Type = "text", ParameterName = "saldo", Text = "1500" }
            }
        };

        var result = _sut.Create(req);

        Assert.NotNull(result.Template.Components);
        var comp = Assert.Single(result.Template.Components!);
        Assert.Equal("body", comp.Type);
        Assert.Equal(2, comp.Parameters.Count);
        Assert.Equal("nombre", comp.Parameters[0].ParameterName);
        Assert.Equal("Juan", comp.Parameters[0].Text);
        Assert.Equal("saldo", comp.Parameters[1].ParameterName);
        Assert.Equal("1500", comp.Parameters[1].Text);
    }

    [Fact]
    public void TextNull_ConvierteACadenaVacia()
    {
        // El controller original hacia Text = parameter.Text ?? "";
        // Verificamos que la factory conserva ese comportamiento para no
        // cambiar el contrato hacia la libreria.
        var req = new TemplateRequest
        {
            Number = "5215512345678",
            TemplateName = "t",
            Parameters = new List<TemplateParameter>
            {
                new() { Type = "text", ParameterName = "x", Text = null }
            }
        };

        var result = _sut.Create(req);

        var p = result.Template.Components![0].Parameters[0];
        Assert.Equal("", p.Text);
    }
}
