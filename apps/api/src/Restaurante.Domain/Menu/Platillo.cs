using Restaurante.Domain.Comun;

namespace Restaurante.Domain.Menu;

public sealed class Platillo : IEntidadDeRestaurante, IEliminable
{
    public const int LongitudMaximaNombre = 60;
    public const int LongitudMaximaNombreCorto = 24;
    public const int LongitudMaximaDescripcion = 300;

    private List<string> _ingredientes = [];

    // Requerido por EF Core.
    private Platillo()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
    }

    private Platillo(Guid id, Guid restauranteId, Guid categoriaId, DatosPlatillo datos, DateTimeOffset creadoEn)
    {
        Id = id;
        RestauranteId = restauranteId;
        Activo = true;
        CreadoEn = creadoEn;
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
        Aplicar(categoriaId, datos);
    }

    public Guid Id { get; private set; }
    public Guid RestauranteId { get; private set; }
    public Guid CategoriaId { get; private set; }
    public string Nombre { get; private set; }
    public string NombreNormalizado { get; private set; }
    public string? NombreCorto { get; private set; }
    public string? NombreCortoNormalizado { get; private set; }
    public string? Descripcion { get; private set; }
    public int Orden { get; private set; }
    public IReadOnlyList<string> Ingredientes => _ingredientes;
    public bool Activo { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset? EliminadaEn { get; private set; }

    public static Platillo Crear(Guid restauranteId, Guid categoriaId, DatosPlatillo datos, DateTimeOffset creadoEn)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return new Platillo(Guid.NewGuid(), restauranteId, categoriaId, datos, creadoEn);
    }

    public void Editar(Guid categoriaId, DatosPlatillo datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        Aplicar(categoriaId, datos);
    }

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;

    public void Eliminar(DateTimeOffset ahora) => EliminadaEn = ahora;

    // Valida todo antes de asignar, para no dejar la entidad a medias si algo falla.
    private void Aplicar(Guid categoriaId, DatosPlatillo datos)
    {
        var nombre = datos.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length is 0 || nombre.Length > LongitudMaximaNombre)
        {
            throw new ArgumentException(
                $"El nombre debe tener entre 1 y {LongitudMaximaNombre} caracteres.", nameof(datos));
        }

        var corto = datos.NombreCorto?.Trim();
        if (string.IsNullOrEmpty(corto))
        {
            corto = null;
        }
        else if (corto.Length > LongitudMaximaNombreCorto)
        {
            throw new ArgumentException(
                $"El nombre corto no puede superar {LongitudMaximaNombreCorto} caracteres.", nameof(datos));
        }

        var descripcion = datos.Descripcion?.Trim();
        if (string.IsNullOrEmpty(descripcion))
        {
            descripcion = null;
        }
        else if (descripcion.Length > LongitudMaximaDescripcion)
        {
            throw new ArgumentException(
                $"La descripción no puede superar {LongitudMaximaDescripcion} caracteres.", nameof(datos));
        }

        if (datos.Orden < 1)
        {
            throw new ArgumentException("El orden debe ser un entero mayor o igual que 1.", nameof(datos));
        }

        var ingredientes = IngredientesPlatillo.Normalizar(datos.Ingredientes);

        CategoriaId = categoriaId;
        Nombre = nombre;
        NombreNormalizado = Categoria.Normalizar(nombre);
        NombreCorto = corto;
        NombreCortoNormalizado = corto is null ? null : Categoria.Normalizar(corto);
        Descripcion = descripcion;
        Orden = datos.Orden;
        _ingredientes = [.. ingredientes];
    }
}
