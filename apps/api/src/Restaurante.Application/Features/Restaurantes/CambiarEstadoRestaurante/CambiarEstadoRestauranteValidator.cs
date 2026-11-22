using FluentValidation;

namespace Restaurante.Application.Features.Restaurantes.CambiarEstadoRestaurante;

public sealed class CambiarEstadoRestauranteValidator : AbstractValidator<CambiarEstadoRestauranteCommand>
{
    public CambiarEstadoRestauranteValidator()
    {
        RuleFor(x => x.Activo).NotNull().WithMessage("El estado es obligatorio");
    }
}
