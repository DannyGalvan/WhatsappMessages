namespace WhatsappSendMessages.Entities.Response
{
    public class Response<TEntity>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public TEntity? Data { get; set; } = default;
    }
}
