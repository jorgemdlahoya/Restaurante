using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Platillos;

/// <summary>
/// Comprueba la unicidad del nombre y del nombre corto (sin distinguir mayúsculas ni espacios) entre los
/// platillos vigentes del restaurante, y avisa de todos los campos repetidos a la vez.
/// </summary>
public sealed class ComprobadorUnicidadPlatillo(IPlatilloRepository platillos)
{
    public async Task ComprobarAsync(string nombre, string? nombreCorto, Guid? excluirId, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();

        if (await platillos.ExisteNombreAsync(Categoria.Normalizar(nombre), excluirId, ct))
        {
            errores["nombre"] = [MensajesPlatillo.NombreRepetido];
        }

        if (!string.IsNullOrWhiteSpace(nombreCorto)
            && await platillos.ExisteNombreCortoAsync(Categoria.Normalizar(nombreCorto), excluirId, ct))
        {
            errores["nombreCorto"] = [MensajesPlatillo.NombreCortoRepetido];
        }

        if (errores.Count > 0)
        {
            throw new ConflictoCamposException(errores);
        }
    }
}
