using FluentValidation;

namespace Restaurante.Application.Features.Categorias.EditarCategoria;

public sealed class EditarCategoriaValidator : AbstractValidator<EditarCategoriaCommand>
{
    public EditarCategoriaValidator()
    {
        // Los datos se validan sin prefijo ("nombre", no "datos.nombre"): el cuerpo de la API es plano.
        RuleFor(x => x.Datos)
            .SetValidator(new ValidadorDatosCategoria())
            .OverridePropertyName(string.Empty);
    }
}
