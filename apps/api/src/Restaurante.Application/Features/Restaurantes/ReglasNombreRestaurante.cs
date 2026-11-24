using FluentValidation;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Features.Restaurantes;

/// <summary>Regla de nombre de restaurante compartida por el alta y la edición.</summary>
public static class ReglasNombreRestaurante
{
    public static IRuleBuilderOptions<T, string> NombreDeRestauranteValido<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio")
            .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= RestauranteEntidad.LongitudMaximaNombre)
            .WithMessage("El nombre no puede superar 80 caracteres");
}
