using FluentValidation;

namespace Restaurante.Application.Features.Platillos.CambiarEstadoPlatillo;

public sealed class CambiarEstadoPlatilloValidator : AbstractValidator<CambiarEstadoPlatilloCommand>
{
    public CambiarEstadoPlatilloValidator()
    {
        RuleFor(x => x.Activo).NotNull().WithMessage("El estado es obligatorio");
    }
}
