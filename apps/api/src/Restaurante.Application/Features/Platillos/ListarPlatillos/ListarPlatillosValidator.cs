using FluentValidation;
using Restaurante.Application.Features.Categorias.ListarCategorias;

namespace Restaurante.Application.Features.Platillos.ListarPlatillos;

public sealed class ListarPlatillosValidator : AbstractValidator<ListarPlatillosQuery>
{
    public ListarPlatillosValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual que 1");
        RuleFor(x => x.Tamano).InclusiveBetween(1, ListarCategoriasValidator.TamanoMaximo)
            .WithMessage($"El tamaño debe estar entre 1 y {ListarCategoriasValidator.TamanoMaximo}");
    }
}
