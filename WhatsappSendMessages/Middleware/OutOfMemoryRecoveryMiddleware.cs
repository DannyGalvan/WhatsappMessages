namespace WhatsappSendMessages.Middleware
{
    // System.Text.Json cachea la excepcion si falla al construir la metadata de un tipo
    // (JsonSerializerOptions.CachingContext). Un solo OutOfMemoryException puntual durante
    // el primer request deja "envenenado" el cache: cada request posterior relanza el mismo
    // OOM aunque ya haya memoria libre, y el proceso nunca se cae para que IIS lo recicle.
    // Ante un OOM se detiene la app; ANCM (in-process) levanta un proceso limpio en el
    // siguiente request.
    public class OutOfMemoryRecoveryMiddleware(
        RequestDelegate next,
        IHostApplicationLifetime lifetime,
        ILogger<OutOfMemoryRecoveryMiddleware> logger)
    {
        private static int _stopRequested;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (OutOfMemoryException e)
            {
                if (Interlocked.Exchange(ref _stopRequested, 1) == 0)
                {
                    logger.LogCritical(e,
                        "OutOfMemoryException en {Path}. Se detiene la aplicacion para que IIS levante un proceso nuevo.",
                        context.Request.Path);
                    lifetime.StopApplication();
                }

                if (!context.Response.HasStarted)
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            }
        }
    }
}
