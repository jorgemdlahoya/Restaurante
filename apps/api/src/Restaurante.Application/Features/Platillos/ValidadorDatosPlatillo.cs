using FluentValidation;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Platillos;

/// <summary>Reglas de formato de los datos de un platillo (006/RF-02), compartidas por el alta y la edición.</summary>
public sealed class ValidadorDatosPlatillo : AbstractValidator<DatosPlatilloDto>
{
    public ValidadorDatosPlatillo()
    {
        RuleFor(x => x.Nombre)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio")
            .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= Platillo.LongitudMaximaNombre)
            .WithMessage($"El nombre no puede superar {Platillo.LongitudMaximaNombre} caracteres");

        RuleFor(x => x.NombreCorto)
            .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= Platillo.LongitudMaximaNombreCorto)
            .WithMessage($"El nombre corto no puede superar {Platillo.LongitudMaximaNombreCorto} caracteres");

        RuleFor(x => x.Descripcion)
            .Must(d => string.IsNullOrWhiteSpace(d) || d.Trim().Length <= Platillo.LongitudMaximaDescripcion)
            .WithMessage($"La descripción no puede superar {Platillo.LongitudMaximaDescripcion} caracteres");

        RuleFor(x => x.Orden)
            .NotNull().WithMessage("El orden es obligatorio")
            .Must(o => o is null or >= 1).WithMessage("El orden debe ser un entero mayor o igual que 1");

        // Las reglas viven en IngredientesPlatillo (una sola fuente); aquí sólo se traducen a fallos de "ingredientes".
        RuleFor(x => x.Ingredientes).Custom((ingredientes, contexto) =>
        {
            foreach (var error in IngredientesPlatillo.Errores(ingredientes))
            {
                contexto.AddFailure(error);
            }
        });
    }
}
