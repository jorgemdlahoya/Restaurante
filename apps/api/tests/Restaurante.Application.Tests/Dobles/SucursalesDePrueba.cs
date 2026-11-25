using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Dobles;

/// <summary>Ayudas para sembrar sucursales y cumplir la cardinalidad rol ↔ sucursales (004/RF-02) en las pruebas.</summary>
public static class SucursalesDePrueba
{
    /// <summary>
    /// Sucursales mínimas que admite el rol: ninguna para los administradores general y de plataforma, una (un id
    /// cualquiera, no sembrado) para el resto. Para pruebas a las que no les importan las sucursales.
    /// </summary>
    public static IReadOnlyCollection<Guid> Para(Rol rol) =>
        Usuario.CardinalidadValida(rol, 0) ? [] : [Guid.NewGuid()];

    public static Sucursal Crear(Guid restauranteId, string slug, string nombre, string prefijo, string whatsApp) =>
        Sucursal.Crear(
            restauranteId,
            slug,
            new DatosSucursal(nombre, "Calle 1", "+525500000000", whatsApp, null, prefijo, ZonasHorarias.PorDefecto),
            new RelojFijo().Ahora);
}
