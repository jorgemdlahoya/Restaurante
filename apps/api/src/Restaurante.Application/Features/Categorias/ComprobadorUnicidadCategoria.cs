using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Menu;

namespace Restaurante.Application.Features.Categorias;

/// <summary>
/// Comprueba la unicidad del nombre y del nombre corto (sin distinguir mayúsculas ni espacios) entre las
/// categorías vigentes del restaurante, y avisa de todos los campos repetidos a la vez.
/// </summary>
public sealed class ComprobadorUnicidadCategoria(ICategoriaRepository categorias)
{
    public async Task ComprobarAsync(string nombre, string? nombreCorto, Guid? excluirId, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();

        if (await categorias.ExisteNombreAsync(Categoria.Normalizar(nombre), excluirId, ct))
        {
            errores["nombre"] = [MensajesCategoria.NombreRepetido];
        }

        if (!string.IsNullOrWhiteSpace(nombreCorto)
            && await categorias.ExisteNombreCortoAsync(Categoria.Normalizar(nombreCorto), excluirId, ct))
        {
            errores["nombreCorto"] = [MensajesCategoria.NombreCortoRepetido];
        }

        if (errores.Count > 0)
        {
            throw new ConflictoCamposException(errores);
        }
    }
}
