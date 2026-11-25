using System.Text.RegularExpressions;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Features.Usuarios.CrearUsuario;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Infrastructure.Arranque;

/// <summary>Resultado de la inicialización; <see cref="Error"/> nombra la variable que falta, nunca valores.</summary>
public sealed record ResultadoInicializacion(bool Ok, string? Error)
{
    public static ResultadoInicializacion Exito { get; } = new(true, null);

    public static ResultadoInicializacion Fallo(string error) => new(false, error);
}

/// <summary>
/// Crea el administrador de plataforma inicial desde el entorno sólo cuando no existe ninguno. Se ejecuta con
/// contexto de restaurante nulo: el filtro global hace que <c>ExisteAlgunoAsync</c> signifique
/// "ya hay un administrador de plataforma".
/// </summary>
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

        // Mismas reglas que RF-13; el error nombra la variable y el motivo, nunca el valor.
        nombre = nombre.Trim();
        if (!Regex.IsMatch(nombre, CrearUsuarioValidator.PatronNombreUsuario))
        {
            return ResultadoInicializacion.Fallo(
                $"{VariableUsuario} no es válida: debe tener entre 3 y 50 caracteres " +
                "(letras, números, punto, guion y guion bajo).");
        }

        if (contrasena.Length < CrearUsuarioValidator.LongitudMinimaContrasena)
        {
            return ResultadoInicializacion.Fallo(
                $"{VariableContrasena} no es válida: debe tener al menos " +
                $"{CrearUsuarioValidator.LongitudMinimaContrasena} caracteres.");
        }

        usuarios.Agregar(Usuario.Crear(null, nombre, hasher.Hashear(contrasena), Rol.AdministradorPlataforma, [], reloj.Ahora));
        await usuarios.GuardarCambiosAsync(ct);
        return ResultadoInicializacion.Exito;
    }

    private static string? Leer(IReadOnlyDictionary<string, string?> entorno, string clave) =>
        entorno.TryGetValue(clave, out var valor) && !string.IsNullOrWhiteSpace(valor) ? valor : null;
}
