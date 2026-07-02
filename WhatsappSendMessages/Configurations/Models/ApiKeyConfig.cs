namespace WhatsappSendMessages.Configurations.Models
{
    public class ApiKeyConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public List<string> ExcludedPaths { get; set; } = [];
    }
}
