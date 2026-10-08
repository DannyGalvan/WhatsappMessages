using System.Reflection;
using System.Runtime.CompilerServices;
using WhatsappBusiness.CloudApi.Interfaces;
using WhatsappBusiness.CloudApi.Response;

namespace WhatsappSendMessages.Tests.Infrastructure;

/// <summary>
/// Fake de IWhatsAppBusinessClient construido con DispatchProxy para no
/// tener que implementar manualmente la interfaz (~100 metodos). Solo
/// SendTextMessageTemplateAsync hace algo util; el resto lanza
/// NotImplementedException para que cualquier uso accidental en tests
/// falle ruidosamente.
/// </summary>
public class FakeWhatsAppBusinessClient : DispatchProxy
{
    public static IWhatsAppBusinessClient Create() =>
        Create<IWhatsAppBusinessClient, FakeWhatsAppBusinessClient>()!;

    public WhatsAppResponse? NextResponse { get; set; }
    public Exception? NextException { get; set; }
    public int SendTextMessageTemplateCallCount { get; private set; }

    /// <summary>Limpia el estado entre tests. La factory lo invoca antes de cada
    /// request porque las suites comparten la misma instancia via IClassFixture.</summary>
    public void Reset()
    {
        NextResponse = null;
        NextException = null;
        SendTextMessageTemplateCallCount = 0;
    }

    private static readonly WhatsAppResponse DefaultResponse = new()
    {
        MessagingProduct = "whatsapp",
        Contacts = new List<Contact>
        {
            new() { Input = "5215512345678", WaId = "5215512345678" }
        },
        Messages = new List<Message>
        {
            new() { Id = "wamid.test-default-id" }
        }
    };

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        switch (targetMethod?.Name)
        {
            case "SendTextMessageTemplateAsync":
                SendTextMessageTemplateCallCount++;
                if (NextException is not null) throw NextException;
                return Task.FromResult(NextResponse ?? DefaultResponse);

            // IWhatsAppBusinessClient hereda de IDisposable. Si InvokeAsync lo
            // llama, lo dejamos pasar sin hacer nada.
            case "Dispose":
                return null;

            default:
                throw new NotImplementedException(
                    $"FakeWhatsAppBusinessClient no implementa {targetMethod?.Name}. " +
                    $"Anade el caso en Invoke() solo si un test lo necesita.");
        }
    }
}
