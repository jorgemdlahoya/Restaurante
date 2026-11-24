using Restaurante.Domain.Comun;

namespace Restaurante.Domain.Sucursales;

/// <summary>Horario de un día de la semana de una sucursal (sin horas = cerrado).</summary>
public sealed class HorarioDia : IEntidadDeSucursal
{
    // Requerido por EF Core.
    private HorarioDia()
    {
    }

    private HorarioDia(Guid restauranteId, Guid sucursalId, DiaSemana dia, TimeOnly? apertura, TimeOnly? cierre)
    {
        RestauranteId = restauranteId;
        SucursalId = sucursalId;
        Dia = dia;
        Apertura = apertura;
        Cierre = cierre;
    }

    public Guid RestauranteId { get; private set; }
    public Guid SucursalId { get; private set; }
    public DiaSemana Dia { get; private set; }
    public TimeOnly? Apertura { get; private set; }
    public TimeOnly? Cierre { get; private set; }

    public bool Cerrado => Apertura is null;

    /// <summary>Verdadero si el turno termina al día siguiente (p. ej. 18:00 a 02:00).</summary>
    public bool CruzaMedianoche => Apertura is not null && Cierre < Apertura;

    public static HorarioDia Crear(Guid restauranteId, Guid sucursalId, DiaSemana dia, TimeOnly? apertura, TimeOnly? cierre)
    {
        Validar(apertura, cierre);
        return new HorarioDia(restauranteId, sucursalId, dia, apertura, cierre);
    }

    public void Cambiar(TimeOnly? apertura, TimeOnly? cierre)
    {
        Validar(apertura, cierre);
        Apertura = apertura;
        Cierre = cierre;
    }

    public static IReadOnlyList<HorarioDia> SemanaCerrada(Guid restauranteId, Guid sucursalId) =>
        Enum.GetValues<DiaSemana>()
            .Select(d => new HorarioDia(restauranteId, sucursalId, d, null, null))
            .ToList();

    private static void Validar(TimeOnly? apertura, TimeOnly? cierre)
    {
        if (apertura is null != cierre is null)
        {
            throw new ArgumentException("Indica apertura y cierre, o ninguno para dejar el día cerrado.");
        }

        if (apertura is not null && apertura == cierre)
        {
            throw new ArgumentException("La apertura y el cierre no pueden ser iguales.");
        }
    }
}
