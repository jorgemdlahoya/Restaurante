using FluentValidation;

namespace Restaurante.Application.Features.Restaurantes.EditarRestaurante;

public sealed class EditarRestauranteValidator : AbstractValidator<EditarRestauranteCommand>
{
    public EditarRestauranteValidator()
    {
        RuleFor(x => x.Nombre).NombreDeRestauranteValido();

        When(x => x.Paleta is not null, () =>
            RuleFor(x => x.Paleta!).SetValidator(new ValidadorPaleta()));
    }
}
