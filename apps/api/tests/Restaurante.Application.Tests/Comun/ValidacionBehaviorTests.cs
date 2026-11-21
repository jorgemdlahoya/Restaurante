using FluentValidation;
using Restaurante.Application.Comun.Behaviors;

namespace Restaurante.Application.Tests.Comun;

public class ValidacionBehaviorTests
{
    private static ValidacionBehavior<PruebaCommand, string> Crear() =>
        new(new IValidator<PruebaCommand>[] { new PruebaCommandValidator() });

    [Fact(DisplayName = "001/RF-04: un request inválido no llega al handler y lanza ValidationException")]
    public async Task RequestInvalido_NoLlegaAlHandler()
    {
        var handlerLlamado = false;

        await Assert.ThrowsAsync<ValidationException>(() =>
            Crear().Handle(new PruebaCommand("", "corta"), _ =>
            {
                handlerLlamado = true;
                return Task.FromResult("ok");
            }, CancellationToken.None));

        Assert.False(handlerLlamado);
    }

    [Fact(DisplayName = "001/RF-13: se informan todos los errores por campo")]
    public async Task RequestInvalido_InformaTodosLosErrores()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            Crear().Handle(new PruebaCommand("", "corta"), _ => Task.FromResult("ok"), CancellationToken.None));

        Assert.Equal(2, ex.Errors.Count());
        Assert.Contains(ex.Errors, e => e.PropertyName == "Nombre" && e.ErrorMessage == "El nombre es obligatorio");
        Assert.Contains(ex.Errors, e => e.PropertyName == "Contrasena");
    }

    [Fact(DisplayName = "001/RF-04: un request válido llega al handler")]
    public async Task RequestValido_LlegaAlHandler()
    {
        var r = await Crear().Handle(new PruebaCommand("ana", "12345678"), _ => Task.FromResult("listo"),
            CancellationToken.None);

        Assert.Equal("listo", r);
    }
}
