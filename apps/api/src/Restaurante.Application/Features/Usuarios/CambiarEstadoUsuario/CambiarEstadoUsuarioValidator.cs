using FluentValidation;

namespace Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;

public sealed class CambiarEstadoUsuarioValidator : AbstractValidator<CambiarEstadoUsuarioCommand>
{
    public CambiarEstadoUsuarioValidator()
    {
        RuleFor(x => x.Activo)
            .NotNull().WithMessage("El estado (activo) es obligatorio");
    }
}
