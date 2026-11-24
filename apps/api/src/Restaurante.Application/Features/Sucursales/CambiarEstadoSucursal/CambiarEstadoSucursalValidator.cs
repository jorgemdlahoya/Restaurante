using FluentValidation;

namespace Restaurante.Application.Features.Sucursales.CambiarEstadoSucursal;

public sealed class CambiarEstadoSucursalValidator : AbstractValidator<CambiarEstadoSucursalCommand>
{
    public CambiarEstadoSucursalValidator()
    {
        RuleFor(x => x.Activa).NotNull().WithMessage("El estado es obligatorio");
    }
}
