using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Infrastructure.Arranque;

/// <summary>Resultado de la inicialización; <see cref="Error"/> nombra la variable que falta, nunca valores.</summary>
public sealed record ResultadoInicializacion(bool Ok, string? Error)
{
    public static ResultadoInicializacion Exito { get; } = new(true, null);

    public static ResultadoInicializacion Fallo(string error) => new(false, error);
}

/// <summary>Crea el administrador inicial desde el entorno sólo cuando no existe ningún usuario.</summary>
public sealed class InicializadorAdministrador(
    IUsuarioRepository usuarios, IHasherContrasenas hasher, IReloj reloj)
{
    public const string VariableUsuario = "ADMIN_USERNAME";
    public const string VariableContrasena = "ADMIN_PASSWORD";

    public async Task<ResultadoInicializacion> InicializarAsync(
        IReadOnlyDictionary<string, string?> entorno, CancellationToken ct)
    {
        if (await usuarios.ExisteAlgunoAsync(ct))
        {
            return ResultadoInicializacion.Exito;
        }

        var nombre = Leer(entorno, VariableUsuario);
        if (nombre is null)
        {
            return ResultadoInicializacion.Fallo($"Falta la variable de entorno {VariableUsuario}.");
        }

        var contrasena = Leer(entorno, VariableContrasena);
        if (contrasena is null)
        {
            return ResultadoInicializacion.Fallo($"Falta la variable de entorno {VariableContrasena}.");
        }

        usuarios.Agregar(Usuario.Crear(nombre.Trim(), hasher.Hashear(contrasena), Rol.Administrador, reloj.Ahora));
        await usuarios.GuardarCambiosAsync(ct);
        return ResultadoInicializacion.Exito;
    }

    private static string? Leer(IReadOnlyDictionary<string, string?> entorno, string clave) =>
        entorno.TryGetValue(clave, out var valor) && !string.IsNullOrWhiteSpace(valor) ? valor : null;
}
