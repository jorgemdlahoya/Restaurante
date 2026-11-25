using FluentValidation;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Categorias;

/// <summary>Reglas de formato de los datos de una categoría (005/RF-02), compartidas por el alta y la edición.</summary>
public sealed class ValidadorDatosCategoria : AbstractValidator<DatosCategoriaDto>
{
    public ValidadorDatosCategoria()
    {
        RuleFor(x => x.Nombre)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio")
            .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= Categoria.LongitudMaximaNombre)
            .WithMessage($"El nombre no puede superar {Categoria.LongitudMaximaNombre} caracteres");

        RuleFor(x => x.NombreCorto)
            .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= Categoria.LongitudMaximaNombreCorto)
            .WithMessage($"El nombre corto no puede superar {Categoria.LongitudMaximaNombreCorto} caracteres");

        RuleFor(x => x.Orden)
            .NotNull().WithMessage("El orden es obligatorio")
            .Must(o => o is null or >= 1).WithMessage("El orden debe ser un entero mayor o igual que 1");
    }
}
