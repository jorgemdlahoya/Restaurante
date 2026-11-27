using FluentValidation;

namespace Restaurante.Application.Features.Categorias.CambiarEstadoCategoria;

public sealed class CambiarEstadoCategoriaValidator : AbstractValidator<CambiarEstadoCategoriaCommand>
{
    public CambiarEstadoCategoriaValidator()
    {
        RuleFor(x => x.Activa).NotNull().WithMessage("El estado es obligatorio");
    }
}
