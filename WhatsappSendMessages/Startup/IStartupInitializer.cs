namespace WhatsappSendMessages.Startup
{
    /// <summary>
    /// Punto de extension OCP para codigo que debe correr una sola vez al
    /// arrancar la app (seed de datos, migraciones, etc). Cada initializer
    /// se registra como scoped y el runner los invoca en orden ascendente
    /// de Order. Agregar un initializer nuevo no requiere tocar al runner.
    /// </summary>
    public interface IStartupInitializer
    {
        int Order { get; }
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}
