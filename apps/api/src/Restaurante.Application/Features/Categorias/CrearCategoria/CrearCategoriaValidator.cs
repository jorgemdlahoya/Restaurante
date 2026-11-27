using FluentValidation;

namespace Restaurante.Application.Features.Categorias.CrearCategoria;

public sealed class CrearCategoriaValidator : AbstractValidator<CrearCategoriaCommand>
{
    public CrearCategoriaValidator()
    {
        // Los datos se validan sin prefijo ("nombre", no "datos.nombre"): el cuerpo de la API es plano.
        RuleFor(x => x.Datos)
            .SetValidator(new ValidadorDatosCategoria())
            .OverridePropertyName(string.Empty);
    }
}
