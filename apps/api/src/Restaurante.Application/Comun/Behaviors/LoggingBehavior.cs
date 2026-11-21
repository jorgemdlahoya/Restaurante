using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Restaurante.Application.Comun.Behaviors;

/// <summary>Registra nombre del request, duración y resultado. Nunca el contenido del request.</summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var nombre = typeof(TRequest).Name;
        var reloj = Stopwatch.StartNew();
        try
        {
            var respuesta = await next(cancellationToken);
            _logger.LogInformation("{Request} terminó en {Ms} ms: {Resultado}", nombre, reloj.ElapsedMilliseconds, "ok");
            return respuesta;
        }
        catch
        {
            _logger.LogError("{Request} terminó en {Ms} ms: {Resultado}", nombre, reloj.ElapsedMilliseconds, "error");
            throw;
        }
    }
}
