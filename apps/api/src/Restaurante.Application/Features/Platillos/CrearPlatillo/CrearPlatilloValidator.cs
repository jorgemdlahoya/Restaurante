using FluentValidation;

namespace Restaurante.Application.Features.Platillos.CrearPlatillo;

public sealed class CrearPlatilloValidator : AbstractValidator<CrearPlatilloCommand>
{
    public CrearPlatilloValidator()
    {
        // Los datos se validan sin prefijo ("nombre", no "datos.nombre"): el cuerpo de la API es plano.
        RuleFor(x => x.Datos)
            .SetValidator(new ValidadorDatosPlatillo())
            .OverridePropertyName(string.Empty);
    }
}
