using FluentValidation;
using Restaurante.Application.Features.Usuarios.CrearUsuario;

namespace Restaurante.Application.Features.Usuarios.CambiarContrasenaUsuario;

public sealed class CambiarContrasenaUsuarioValidator : AbstractValidator<CambiarContrasenaUsuarioCommand>
{
    public CambiarContrasenaUsuarioValidator()
    {
        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(CrearUsuarioValidator.LongitudMinimaContrasena)
            .WithMessage("La contraseña debe tener al menos 8 caracteres");
    }
}
