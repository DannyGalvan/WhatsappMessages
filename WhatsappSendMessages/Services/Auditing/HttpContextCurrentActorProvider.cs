using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace WhatsappSendMessages.Services.Auditing
{
    public class HttpContextCurrentActorProvider(IHttpContextAccessor accessor) : ICurrentActorProvider
    {
        public string GetActor()
        {
            var user = accessor.HttpContext?.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated) return "system";

            // ApiKeyAuthenticationHandler pone ClaimTypes.NameIdentifier = id y
            // ClaimTypes.Name = name; los usamos para componer el actor.
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = user.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name)) return "system";

            return $"apikey:{id}:{name}";
        }
    }
}
