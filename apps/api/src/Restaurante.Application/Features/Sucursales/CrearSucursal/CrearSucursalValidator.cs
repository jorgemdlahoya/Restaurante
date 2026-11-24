using System.Text.RegularExpressions;
using FluentValidation;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.CrearSucursal;

public sealed partial class CrearSucursalValidator : AbstractValidator<CrearSucursalCommand>
{
    public CrearSucursalValidator()
    {
        RuleFor(x => x.Slug)
            .Must(s => s is not null && PatronSlug().IsMatch(s))
            .WithMessage("El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones");

        // Los datos se validan sin prefijo ("nombre", no "datos.nombre"): el cuerpo de la API es plano.
        RuleFor(x => x.Datos)
            .SetValidator(new ValidadorDatosSucursal())
            .OverridePropertyName(string.Empty);
    }

    [GeneratedRegex(Sucursal.PatronSlug)]
    private static partial Regex PatronSlug();
}
