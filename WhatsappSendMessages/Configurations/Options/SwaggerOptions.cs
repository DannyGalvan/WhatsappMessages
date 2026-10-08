namespace WhatsappSendMessages.Configurations.Options
{
    /// <summary>
    /// Habilita/deshabilita Swagger. Default true para mantener el comportamiento
    /// actual si la seccion "Swagger" no existe en appsettings.
    /// </summary>
    public class SwaggerOptions
    {
        public bool Enabled { get; set; } = true;
    }
}
