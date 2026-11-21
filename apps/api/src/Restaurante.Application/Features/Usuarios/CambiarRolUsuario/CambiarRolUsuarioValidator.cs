using FluentValidation;
using Restaurante.Application.Features.Usuarios.CrearUsuario;

namespace Restaurante.Application.Features.Usuarios.CambiarRolUsuario;

public sealed class CambiarRolUsuarioValidator : AbstractValidator<CambiarRolUsuarioCommand>
{
    public CambiarRolUsuarioValidator()
    {
        RuleFor(x => x.Rol)
            .Must(rol => rol is not null && CrearUsuarioValidator.RolesValidos.Contains(rol, StringComparer.Ordinal))
            .WithMessage("El rol debe ser mesero, cocina, caja o administrador");
    }
}
