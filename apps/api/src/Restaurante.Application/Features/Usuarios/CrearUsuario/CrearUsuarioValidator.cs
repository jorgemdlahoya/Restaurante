using FluentValidation;

namespace Restaurante.Application.Features.Usuarios.CrearUsuario;

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioCommand>
{
    internal static readonly string[] RolesValidos = ["administrador_general", "administrador_sucursal", "cajero", "cocinero"];

    /// <summary>Formato del nombre de usuario (RF-13); compartido con el administrador inicial (RF-10).</summary>
    public const string PatronNombreUsuario = "^[A-Za-z0-9._-]{3,50}$";

    /// <summary>Longitud mínima de la contraseña (RF-13); compartida con el administrador inicial (RF-10).</summary>
    public const int LongitudMinimaContrasena = 8;

    public CrearUsuarioValidator()
    {
        RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .Matches(PatronNombreUsuario)
            .WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres: letras, números, punto, guion y guion bajo");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(LongitudMinimaContrasena).WithMessage("La contraseña debe tener al menos 8 caracteres");

        RuleFor(x => x.Rol)
            .Must(rol => rol is not null && RolesValidos.Contains(rol, StringComparer.Ordinal))
            .WithMessage("El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero");
    }
}
