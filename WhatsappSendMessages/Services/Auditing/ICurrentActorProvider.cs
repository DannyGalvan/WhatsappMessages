namespace WhatsappSendMessages.Services.Auditing
{
    /// <summary>
    /// Devuelve el identificador del "actor" que origina un cambio en BD:
    /// "apikey:{id}:{name}" si hay un request autenticado, o "system" si el
    /// cambio ocurre fuera de un request (inicializadores, etc).
    /// </summary>
    public interface ICurrentActorProvider
    {
        string GetActor();
    }
}
