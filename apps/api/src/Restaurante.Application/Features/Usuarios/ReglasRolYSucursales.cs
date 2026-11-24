using System.Linq.Expressions;
using FluentValidation;
using Restaurante.Application.Comun;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios;

/// <summary>
/// Reglas de formato de rol y sucursales (004/RF-02) que comparten el alta (<c>CrearUsuarioValidator</c>) y la
/// edición (<c>EditarUsuarioValidator</c>, RF-04).
/// </summary>
internal static class ReglasRolYSucursales
{
    /// <summary>Roles que se asignan por la API de usuarios del restaurante (nunca <c>administrador_plataforma</c>).</summary>
    internal static readonly string[] RolesValidos = ["administrador_general", "administrador_sucursal", "cajero", "cocinero"];

    /// <summary>
    /// Rol del conjunto exacto y sucursales por slug: ninguna vacía, sin repetir (sin distinguir mayúsculas) y con la
    /// cardinalidad del rol; la cardinalidad sólo se comprueba con un rol válido.
    /// </summary>
    public static void Aplicar<T>(
        AbstractValidator<T> validador,
        Expression<Func<T, string>> rol,
        Expression<Func<T, IReadOnlyList<string>>> sucursales)
    {
        var rolDe = rol.Compile();

        validador.RuleFor(rol)
            .Must(r => r is not null && RolesValidos.Contains(r, StringComparer.Ordinal))
            .WithMessage("El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero");

        validador.RuleFor(sucursales)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Las sucursales son obligatorias")
            .Must(s => s.All(slug => !string.IsNullOrWhiteSpace(slug)))
            .WithMessage("Cada sucursal se indica por su slug")
            .Must(s => s.Select(slug => slug.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == s.Count)
            .WithMessage("Las sucursales no pueden repetirse")
            .Must((x, s) => !RolExtensiones.TryDesdeTexto(rolDe(x), out var r) || Usuario.CardinalidadValida(r, s.Count))
            .WithMessage(x => MensajeCardinalidad(rolDe(x)));
    }

    private static string MensajeCardinalidad(string rol) => rol switch
    {
        "administrador_general" => "El administrador general no lleva sucursales",
        "administrador_sucursal" => "El administrador de sucursal debe tener exactamente una sucursal",
        _ => "El cajero y el cocinero deben tener al menos una sucursal",
    };
}
