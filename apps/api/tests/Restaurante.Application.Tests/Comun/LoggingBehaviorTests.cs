using Microsoft.Extensions.Logging;
using Restaurante.Application.Comun.Behaviors;

namespace Restaurante.Application.Tests.Comun;

public class LoggingBehaviorTests
{
    private static readonly PruebaCommand Peticion = new("ana", "secreta123");

    [Fact(DisplayName = "001/RF-21: registra nombre, duración y resultado")]
    public async Task RegistraNombreDuracionYResultado()
    {
        var logger = new LoggerGrabador<LoggingBehavior<PruebaCommand, string>>();
        var behavior = new LoggingBehavior<PruebaCommand, string>(logger);

        await behavior.Handle(Peticion, _ => Task.FromResult("x"), CancellationToken.None);

        var entrada = Assert.Single(logger.Entradas);
        Assert.Equal(LogLevel.Information, entrada.Nivel);
        Assert.Matches(@"^PruebaCommand terminó en \d+ ms: ok$", entrada.Mensaje);
    }

    [Fact(DisplayName = "001/RF-21: no registra el contenido del request")]
    public async Task NoRegistraContenidoDelRequest()
    {
        var logger = new LoggerGrabador<LoggingBehavior<PruebaCommand, string>>();
        var behavior = new LoggingBehavior<PruebaCommand, string>(logger);

        await behavior.Handle(Peticion, _ => Task.FromResult("x"), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(Peticion, _ => throw new InvalidOperationException("fallo"), CancellationToken.None));

        Assert.NotEmpty(logger.Entradas);
        Assert.All(logger.Entradas, e =>
        {
            Assert.DoesNotContain("secreta123", e.Mensaje);
            Assert.DoesNotContain("ana", e.Mensaje);
        });
    }

    [Fact(DisplayName = "001/RF-21: registra error y relanza la excepción")]
    public async Task RegistraErrorYRelanza()
    {
        var logger = new LoggerGrabador<LoggingBehavior<PruebaCommand, string>>();
        var behavior = new LoggingBehavior<PruebaCommand, string>(logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(Peticion, _ => throw new InvalidOperationException("fallo"), CancellationToken.None));

        Assert.Equal("fallo", ex.Message);
        var entrada = Assert.Single(logger.Entradas);
        Assert.Matches(@"^PruebaCommand terminó en \d+ ms: error$", entrada.Mensaje);
    }
}
