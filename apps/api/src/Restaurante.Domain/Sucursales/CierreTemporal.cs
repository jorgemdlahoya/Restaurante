using Restaurante.Domain.Comun;

namespace Restaurante.Domain.Sucursales;

/// <summary>Cierre temporal de una sucursal; <c>Inicio</c> y <c>Fin</c> están en UTC.</summary>
public sealed class CierreTemporal : IEntidadDeSucursal
{
    public const int LongitudMaximaMotivo = 120;

    // Requerido por EF Core.
    private CierreTemporal()
    {
    }

    private CierreTemporal(
        Guid restauranteId, Guid sucursalId, DateTimeOffset inicio, DateTimeOffset fin, string? motivo, DateTimeOffset creadoEn)
    {
        Id = Guid.NewGuid();
        RestauranteId = restauranteId;
        SucursalId = sucursalId;
        Inicio = inicio;
        Fin = fin;
        Motivo = motivo;
        CreadoEn = creadoEn;
    }

    public Guid Id { get; private set; }
    public Guid RestauranteId { get; private set; }
    public Guid SucursalId { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fin { get; private set; }
    public string? Motivo { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset? CanceladoEn { get; private set; }

    public static CierreTemporal Crear(
        Guid restauranteId, Guid sucursalId, DateTimeOffset inicio, DateTimeOffset fin, string? motivo, DateTimeOffset ahora)
    {
        if (fin <= inicio)
        {
            throw new ArgumentException("El fin debe ser posterior al inicio.", nameof(fin));
        }

        if (fin <= ahora)
        {
            throw new ArgumentException("El fin debe ser posterior al momento actual.", nameof(fin));
        }

        var recortado = motivo?.Trim();
        if (recortado is { Length: > LongitudMaximaMotivo })
        {
            throw new ArgumentException($"El motivo no puede exceder {LongitudMaximaMotivo} caracteres.", nameof(motivo));
        }

        return new CierreTemporal(restauranteId, sucursalId, inicio, fin, string.IsNullOrEmpty(recortado) ? null : recortado, ahora);
    }

    /// <summary>Cancela el cierre: si aún no empezó queda sin duración; si no, termina ahora.</summary>
    public void Cancelar(DateTimeOffset ahora)
    {
        if (ahora < Inicio)
        {
            Inicio = ahora;
        }

        Fin = ahora;
        CanceladoEn = ahora;
    }

    public bool AplicaEn(DateTimeOffset instante) => Inicio <= instante && instante < Fin;
}
