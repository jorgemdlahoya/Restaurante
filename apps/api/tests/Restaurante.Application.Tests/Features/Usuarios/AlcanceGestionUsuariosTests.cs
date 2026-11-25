using Restaurante.Application.Features.Usuarios;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

public class AlcanceGestionUsuariosTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private static readonly Guid Centro = Guid.NewGuid();
    private static readonly Guid Norte = Guid.NewGuid();
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Usuario Crear(Rol rol, params Guid[] sucursales) =>
        Usuario.Crear(RestauranteId, "u" + Guid.NewGuid().ToString("N")[..6], "hash:x", rol, sucursales, Ahora);

    private static ActorUsuarios AdminSucursal(bool activa = true) =>
        new(Guid.NewGuid(), Rol.AdministradorSucursal, Centro, activa);

    private static ActorUsuarios AdminGeneral() => new(Guid.NewGuid(), Rol.AdministradorGeneral, null, false);

    [Fact(DisplayName = "004/RF-07: el administrador general ve y gestiona a todos")]
    public void General_ve_y_gestiona_todo()
    {
        var actor = AdminGeneral();
        foreach (var u in new[] { Crear(Rol.AdministradorGeneral), Crear(Rol.AdministradorSucursal, Norte), Crear(Rol.Cajero, Centro, Norte) })
        {
            Assert.True(AlcanceGestionUsuarios.PuedeVer(actor, u));
            Assert.True(AlcanceGestionUsuarios.PuedeGestionar(actor, u));
        }
        Assert.True(AlcanceGestionUsuarios.PuedeOperar(actor));
    }

    [Fact(DisplayName = "004/RF-07: el administrador de sucursal ve a quien tiene su sucursal, sin administradores generales")]
    public void AdminSucursal_ve_a_quien_tiene_su_sucursal()
    {
        var actor = AdminSucursal();

        Assert.True(AlcanceGestionUsuarios.PuedeVer(actor, Crear(Rol.Cajero, Centro)));
        Assert.True(AlcanceGestionUsuarios.PuedeVer(actor, Crear(Rol.Cocinero, Centro, Norte)));
        Assert.True(AlcanceGestionUsuarios.PuedeVer(actor, Crear(Rol.AdministradorSucursal, Centro)));
        Assert.False(AlcanceGestionUsuarios.PuedeVer(actor, Crear(Rol.Cajero, Norte)));
        Assert.False(AlcanceGestionUsuarios.PuedeVer(actor, Crear(Rol.AdministradorGeneral)));
    }

    [Theory(DisplayName = "004/RF-08: el administrador de sucursal sólo asigna cajero o cocinero con su sucursal")]
    [InlineData(Rol.Cajero, "centro", true)]
    [InlineData(Rol.Cocinero, "centro", true)]
    [InlineData(Rol.Cajero, "norte", false)]
    [InlineData(Rol.Cajero, "centro,norte", false)]
    [InlineData(Rol.Cajero, "", false)]
    [InlineData(Rol.AdministradorSucursal, "centro", false)]
    [InlineData(Rol.AdministradorGeneral, "", false)]
    [InlineData(Rol.AdministradorPlataforma, "", false)]
    public void AdminSucursal_asigna_solo_su_sucursal(Rol rol, string sucursales, bool esperado)
    {
        var ids = sucursales.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s == "centro" ? Centro : Norte).ToArray();

        Assert.Equal(esperado, AlcanceGestionUsuarios.PuedeAsignar(AdminSucursal(), rol, ids));
    }

    [Fact(DisplayName = "004/RF-08: el administrador general asigna cualquier rol y sucursales")]
    public void General_asigna_cualquier_cosa()
    {
        Assert.True(AlcanceGestionUsuarios.PuedeAsignar(AdminGeneral(), Rol.Cajero, [Centro, Norte]));
        Assert.True(AlcanceGestionUsuarios.PuedeAsignar(AdminGeneral(), Rol.AdministradorSucursal, [Norte]));
    }

    [Theory(DisplayName = "004/RF-09: el administrador de sucursal gestiona al cajero exclusivo de su sucursal")]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.Cocinero)]
    public void AdminSucursal_gestiona_personal_exclusivo(Rol rol)
    {
        Assert.True(AlcanceGestionUsuarios.PuedeGestionar(AdminSucursal(), Crear(rol, Centro)));
    }

    [Fact(DisplayName = "004/RF-10: no gestiona al personal compartido, a otro administrador ni a sí mismo")]
    public void AdminSucursal_no_gestiona_compartido_ni_administradores_ni_a_si_mismo()
    {
        var actor = AdminSucursal();

        Assert.False(AlcanceGestionUsuarios.PuedeGestionar(actor, Crear(Rol.Cajero, Centro, Norte)));
        Assert.False(AlcanceGestionUsuarios.PuedeGestionar(actor, Crear(Rol.Cajero, Norte)));
        Assert.False(AlcanceGestionUsuarios.PuedeGestionar(actor, Crear(Rol.AdministradorSucursal, Centro)));
        Assert.False(AlcanceGestionUsuarios.PuedeGestionar(actor, Crear(Rol.AdministradorGeneral)));

        var yo = Crear(Rol.AdministradorSucursal, Centro);
        Assert.False(AlcanceGestionUsuarios.PuedeGestionar(actor with { Id = yo.Id }, yo));
        var yoCajero = Crear(Rol.Cajero, Centro);
        Assert.False(AlcanceGestionUsuarios.PuedeGestionar(actor with { Id = yoCajero.Id }, yoCajero));
    }

    [Theory(DisplayName = "004/RF-11: cajero y cocinero no operan sobre usuarios")]
    [InlineData(Rol.Cajero)]
    [InlineData(Rol.Cocinero)]
    [InlineData(Rol.AdministradorPlataforma)]
    public void Otros_roles_no_operan(Rol rol)
    {
        var actor = new ActorUsuarios(Guid.NewGuid(), rol, Centro, true);
        var objetivo = Crear(Rol.Cajero, Centro);

        Assert.False(AlcanceGestionUsuarios.PuedeOperar(actor));
        Assert.False(AlcanceGestionUsuarios.PuedeVer(actor, objetivo));
        Assert.False(AlcanceGestionUsuarios.PuedeGestionar(actor, objetivo));
        Assert.False(AlcanceGestionUsuarios.PuedeAsignar(actor, Rol.Cajero, [Centro]));
    }

    [Fact(DisplayName = "004/RF-26: con su sucursal inactiva el administrador de sucursal no opera")]
    public void AdminSucursal_con_sucursal_inactiva_no_opera()
    {
        var actor = AdminSucursal(activa: false);
        var objetivo = Crear(Rol.Cajero, Centro);

        Assert.False(AlcanceGestionUsuarios.PuedeOperar(actor));
        Assert.False(AlcanceGestionUsuarios.PuedeVer(actor, objetivo));
        Assert.False(AlcanceGestionUsuarios.PuedeGestionar(actor, objetivo));
        Assert.False(AlcanceGestionUsuarios.PuedeAsignar(actor, Rol.Cajero, [Centro]));
        Assert.True(AlcanceGestionUsuarios.PuedeOperar(AdminSucursal()));
    }

    [Fact(DisplayName = "004/RF-26: el administrador de sucursal sin sucursal propia no opera")]
    public void AdminSucursal_sin_sucursal_no_opera()
    {
        var actor = new ActorUsuarios(Guid.NewGuid(), Rol.AdministradorSucursal, null, true);

        Assert.False(AlcanceGestionUsuarios.PuedeOperar(actor));
        Assert.False(AlcanceGestionUsuarios.PuedeVer(actor, Crear(Rol.Cajero, Guid.NewGuid())));
    }
}
