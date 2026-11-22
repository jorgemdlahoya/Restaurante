using System.Text.RegularExpressions;
using FluentValidation;
using Restaurante.Application.Features.Usuarios.CrearUsuario;

namespace Restaurante.Application.Features.Restaurantes.CrearRestaurante;

public sealed partial class CrearRestauranteValidator : AbstractValidator<CrearRestauranteCommand>
{
    public CrearRestauranteValidator()
    {
        RuleFor(x => x.Nombre).NombreDeRestauranteValido();

        // PatronSlug usa `$`, que en .NET acepta un "\n" final; aquí se ancla con \z.
        RuleFor(x => x.Slug)
            .Must(s => s is not null && SlugEstricto().IsMatch(s))
            .WithMessage("El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones");

        When(x => x.Paleta is not null, () =>
            RuleFor(x => x.Paleta!).SetValidator(new ValidadorPaleta()));

        RuleFor(x => x.Administrador.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .Matches(CrearUsuarioValidator.PatronNombreUsuario)
            .WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres: letras, números, punto, guion y guion bajo");

        RuleFor(x => x.Administrador.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(CrearUsuarioValidator.LongitudMinimaContrasena)
            .WithMessage("La contraseña debe tener al menos 8 caracteres");
    }

    [GeneratedRegex(@"^[a-z0-9-]{3,40}\z")]
    private static partial Regex SlugEstricto();
}
