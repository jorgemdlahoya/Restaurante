using FluentValidation;

namespace Restaurante.Application.Features.Auth.IniciarSesion;

public sealed class IniciarSesionValidator : AbstractValidator<IniciarSesionCommand>
{
    public IniciarSesionValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().WithMessage("El nombre de usuario es obligatorio");
        RuleFor(x => x.Contrasena).NotEmpty().WithMessage("La contraseña es obligatoria");
    }
}
