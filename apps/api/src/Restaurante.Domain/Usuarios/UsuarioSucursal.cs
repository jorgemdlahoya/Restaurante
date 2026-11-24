using Restaurante.Domain.Comun;

namespace Restaurante.Domain.Usuarios;

/// <summary>Asignación de un usuario a una sucursal de su restaurante (004/RF-02).</summary>
public sealed class UsuarioSucursal : IEntidadDeRestaurante
{
    private UsuarioSucursal()
    {
    }

    internal UsuarioSucursal(Guid usuarioId, Guid sucursalId, Guid restauranteId)
    {
        UsuarioId = usuarioId;
        SucursalId = sucursalId;
        RestauranteId = restauranteId;
    }

    public Guid UsuarioId { get; private set; }
    public Guid SucursalId { get; private set; }
    public Guid RestauranteId { get; private set; }
}
