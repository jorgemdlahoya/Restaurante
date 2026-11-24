using FluentValidation;

namespace Restaurante.Application.Features.Sucursales.EditarSucursal;

public sealed class EditarSucursalValidator : AbstractValidator<EditarSucursalCommand>
{
    public EditarSucursalValidator()
    {
        // Los datos se validan sin prefijo ("nombre", no "datos.nombre"): el cuerpo de la API es plano.
        RuleFor(x => x.Datos)
            .SetValidator(new ValidadorDatosSucursal())
            .OverridePropertyName(string.Empty);
    }
}
