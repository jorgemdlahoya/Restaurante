using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Comun.Behaviors;

namespace Restaurante.Application.Tests.Comun;

public class DependencyInjectionTests
{
    [Fact(DisplayName = "001/RF-04: AddApplicationServices registra los behaviours (validación primero) y MediatR")]
    public void RegistraBehavioursEnOrden()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        using var provider = services.BuildServiceProvider();

        var behaviours = provider.GetServices<IPipelineBehavior<PruebaCommand, string>>().ToList();

        Assert.Equal(2, behaviours.Count);
        Assert.IsType<ValidacionBehavior<PruebaCommand, string>>(behaviours[0]);
        Assert.IsType<LoggingBehavior<PruebaCommand, string>>(behaviours[1]);
        Assert.NotNull(provider.GetService<IMediator>());
    }
}
