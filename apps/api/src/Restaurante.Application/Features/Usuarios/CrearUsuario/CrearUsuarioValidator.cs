using FluentValidation;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioCommand>
{
    internal static readonly string[] RolesValidos = ["mesero", "cocina", "caja", "administrador"];

    public CrearUsuarioValidator()
    {
        RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .Matches("^[A-Za-z0-9._-]{3,50}$")
            .WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres: letras, números, punto, guion y guion bajo");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres");

        RuleFor(x => x.Rol)
            .Must(rol => rol is not null && RolesValidos.Contains(rol, StringComparer.Ordinal))
            .WithMessage("El rol debe ser mesero, cocina, caja o administrador");
    }
}
