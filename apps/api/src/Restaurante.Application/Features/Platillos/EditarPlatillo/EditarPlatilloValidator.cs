using FluentValidation;

namespace Restaurante.Application.Features.Platillos.EditarPlatillo;

public sealed class EditarPlatilloValidator : AbstractValidator<EditarPlatilloCommand>
{
    public EditarPlatilloValidator()
    {
        RuleFor(x => x.CategoriaId).NotNull().WithMessage("La categoría es obligatoria");

        // Los datos se validan sin prefijo ("nombre", no "datos.nombre"): el cuerpo de la API es plano.
        RuleFor(x => x.Datos)
            .SetValidator(new ValidadorDatosPlatillo())
            .OverridePropertyName(string.Empty);
    }
}
