using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Features.Usuarios;

/// <summary>
/// Reglas puras de alcance de la gestión de usuarios (004/RF-07..RF-11, RF-26): qué usuarios ve y
/// gestiona el actor, y qué rol y sucursales puede asignar.
/// </summary>
public static class AlcanceGestionUsuarios
{
    /// <summary>El actor puede operar sobre usuarios: general siempre; de sucursal sólo con su sucursal activa.</summary>
    public static bool PuedeOperar(ActorUsuarios actor) => actor.Rol switch
    {
        Rol.AdministradorGeneral => true,
        Rol.AdministradorSucursal => actor.SucursalPropia is not null && actor.SucursalPropiaActiva,
        _ => false,
    };

    public static bool PuedeVer(ActorUsuarios actor, Usuario objetivo)
    {
        if (!PuedeOperar(actor))
        {
            return false;
        }
        if (actor.Rol == Rol.AdministradorGeneral)
        {
            return true;
        }
        return objetivo.Rol != Rol.AdministradorGeneral && TieneSucursalPropia(actor, objetivo);
    }

    public static bool PuedeGestionar(ActorUsuarios actor, Usuario objetivo)
    {
        if (!PuedeOperar(actor))
        {
            return false;
        }
        if (actor.Rol == Rol.AdministradorGeneral)
        {
            return true;
        }
        return objetivo.Id != actor.Id
            && EsPersonalOperativo(objetivo.Rol)
            && objetivo.Sucursales.Count == 1
            && TieneSucursalPropia(actor, objetivo);
    }

    public static bool PuedeAsignar(ActorUsuarios actor, Rol rol, IReadOnlyCollection<Guid> sucursales)
    {
        if (!PuedeOperar(actor))
        {
            return false;
        }
        if (actor.Rol == Rol.AdministradorGeneral)
        {
            return true;
        }
        return EsPersonalOperativo(rol)
            && sucursales.Count == 1
            && sucursales.Contains(actor.SucursalPropia!.Value);
    }

    private static bool EsPersonalOperativo(Rol rol) => rol is Rol.Cajero or Rol.Cocinero;

    private static bool TieneSucursalPropia(ActorUsuarios actor, Usuario objetivo) =>
        objetivo.Sucursales.Contains(actor.SucursalPropia!.Value);
}
