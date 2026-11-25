using FluentValidation;

namespace Restaurante.Application.Features.Categorias.ListarCategorias;

public sealed class ListarCategoriasValidator : AbstractValidator<ListarCategoriasQuery>
{
    public const int TamanoMaximo = 100;

    public ListarCategoriasValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual que 1");
        RuleFor(x => x.Tamano).InclusiveBetween(1, TamanoMaximo).WithMessage("El tamaño debe estar entre 1 y 100");
    }
}
